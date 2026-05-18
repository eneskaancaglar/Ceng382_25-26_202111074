(function () {
    let connection = null;
    let peerConnection = null;
    let localStream = null;
    let remoteAudio = null;
    let orderId = null;
    let isJoined = false;
    let isMuted = false;

    const rtcConfiguration = {
        iceServers: [
            { urls: "stun:stun.l.google.com:19302" }
        ]
    };

    function getElement(id) {
        return document.getElementById(id);
    }

    function setStatus(message, type) {
        const status = getElement("order-call-status");

        if (!status) {
            return;
        }

        status.textContent = message;
        status.className = "order-call-status";

        if (type) {
            status.classList.add(`order-call-status-${type}`);
        }
    }

    function setButtons(state) {
        const joinButton = getElement("order-call-join");
        const startButton = getElement("order-call-start");
        const muteButton = getElement("order-call-mute");
        const hangupButton = getElement("order-call-hangup");

        if (!joinButton || !startButton || !muteButton || !hangupButton) {
            return;
        }

        if (state === "initial") {
            joinButton.disabled = false;
            startButton.disabled = true;
            muteButton.disabled = true;
            hangupButton.disabled = true;
        }

        if (state === "joined") {
            joinButton.disabled = true;
            startButton.disabled = false;
            muteButton.disabled = false;
            hangupButton.disabled = false;
        }

        if (state === "calling") {
            joinButton.disabled = true;
            startButton.disabled = true;
            muteButton.disabled = false;
            hangupButton.disabled = false;
        }

        if (state === "ended") {
            joinButton.disabled = false;
            startButton.disabled = true;
            muteButton.disabled = true;
            hangupButton.disabled = true;
        }
    }

    async function ensureConnection() {
        if (connection) {
            return;
        }

        connection = new signalR.HubConnectionBuilder()
            .withUrl("/orderCallHub")
            .withAutomaticReconnect()
            .build();

        connection.on("PeerJoined", function (payload) {
            const name = payload && payload.userName ? payload.userName : "Other participant";
            setStatus(`${name} joined the voice room. You can start the call.`, "success");
        });

        connection.on("PeerLeft", function () {
            closePeerConnection();
            setStatus("The other participant left the call.", "warning");
            setButtons(isJoined ? "joined" : "initial");
        });

        connection.on("ReceiveOffer", async function (offerJson) {
            try {
                await ensureLocalStream();
                createPeerConnection();

                const offer = JSON.parse(offerJson);

                await peerConnection.setRemoteDescription(offer);

                const answer = await peerConnection.createAnswer();
                await peerConnection.setLocalDescription(answer);

                await connection.invoke("SendAnswer", orderId, JSON.stringify(answer));

                setStatus("Incoming voice call connected.", "success");
                setButtons("calling");
            } catch (error) {
                console.error(error);
                setStatus("Incoming call could not be connected. Check microphone permission.", "danger");
            }
        });

        connection.on("ReceiveAnswer", async function (answerJson) {
            try {
                if (!peerConnection) {
                    return;
                }

                const answer = JSON.parse(answerJson);
                await peerConnection.setRemoteDescription(answer);

                setStatus("Voice call connected.", "success");
                setButtons("calling");
            } catch (error) {
                console.error(error);
                setStatus("Answer could not be processed.", "danger");
            }
        });

        connection.on("ReceiveIceCandidate", async function (candidateJson) {
            try {
                if (!peerConnection) {
                    return;
                }

                const candidate = JSON.parse(candidateJson);
                await peerConnection.addIceCandidate(candidate);
            } catch (error) {
                console.error(error);
            }
        });

        connection.on("CallEnded", function () {
            closePeerConnection();
            setStatus("Voice call ended by the other participant.", "warning");
            setButtons(isJoined ? "joined" : "initial");
        });

        connection.onreconnected(async function () {
            if (isJoined && orderId) {
                await connection.invoke("JoinOrderCall", orderId);
                setStatus("Reconnected to voice room.", "success");
            }
        });

        await connection.start();
    }

    async function ensureLocalStream() {
        if (localStream) {
            return localStream;
        }

        localStream = await navigator.mediaDevices.getUserMedia({
            audio: true,
            video: false
        });

        return localStream;
    }

    function createPeerConnection() {
        if (peerConnection) {
            return;
        }

        peerConnection = new RTCPeerConnection(rtcConfiguration);

        if (localStream) {
            localStream.getTracks().forEach(track => {
                peerConnection.addTrack(track, localStream);
            });
        }

        peerConnection.onicecandidate = async function (event) {
            if (event.candidate && connection && orderId) {
                await connection.invoke(
                    "SendIceCandidate",
                    orderId,
                    JSON.stringify(event.candidate)
                );
            }
        };

        peerConnection.ontrack = function (event) {
            if (!remoteAudio) {
                remoteAudio = getElement("order-call-remote-audio");
            }

            if (remoteAudio && event.streams && event.streams[0]) {
                remoteAudio.srcObject = event.streams[0];
                remoteAudio.play().catch(function () {
                    // Browser may require a user click before audio playback.
                });
            }
        };

        peerConnection.onconnectionstatechange = function () {
            if (!peerConnection) {
                return;
            }

            if (peerConnection.connectionState === "connected") {
                setStatus("Voice call is active.", "success");
                setButtons("calling");
            }

            if (
                peerConnection.connectionState === "failed" ||
                peerConnection.connectionState === "disconnected" ||
                peerConnection.connectionState === "closed"
            ) {
                setStatus("Voice call disconnected.", "warning");
                setButtons(isJoined ? "joined" : "initial");
            }
        };
    }

    function closePeerConnection() {
        if (peerConnection) {
            peerConnection.onicecandidate = null;
            peerConnection.ontrack = null;
            peerConnection.onconnectionstatechange = null;
            peerConnection.close();
            peerConnection = null;
        }

        if (remoteAudio) {
            remoteAudio.srcObject = null;
        }
    }

    async function joinVoiceRoom() {
        try {
            await ensureConnection();
            await ensureLocalStream();

            await connection.invoke("JoinOrderCall", orderId);

            isJoined = true;
            setStatus("You joined the voice room. Open the same order chat with the other account and start the call.", "success");
            setButtons("joined");
        } catch (error) {
            console.error(error);
            setStatus("Could not join voice room. Allow microphone permission and try again.", "danger");
            setButtons("initial");
        }
    }

    async function startCall() {
        try {
            if (!isJoined) {
                await joinVoiceRoom();
            }

            await ensureLocalStream();
            createPeerConnection();

            const offer = await peerConnection.createOffer();
            await peerConnection.setLocalDescription(offer);

            await connection.invoke("SendOffer", orderId, JSON.stringify(offer));

            setStatus("Calling the other participant...", "warning");
            setButtons("calling");
        } catch (error) {
            console.error(error);
            setStatus("Call could not be started. Check microphone permission.", "danger");
            setButtons(isJoined ? "joined" : "initial");
        }
    }

    async function hangUp() {
        try {
            closePeerConnection();

            if (connection && orderId) {
                await connection.invoke("HangUp", orderId);
            }

            setStatus("Voice call ended.", "warning");
            setButtons(isJoined ? "joined" : "initial");
        } catch (error) {
            console.error(error);
            setStatus("Call ended locally.", "warning");
        }
    }

    function toggleMute() {
        if (!localStream) {
            return;
        }

        isMuted = !isMuted;

        localStream.getAudioTracks().forEach(track => {
            track.enabled = !isMuted;
        });

        const muteButton = getElement("order-call-mute");

        if (muteButton) {
            muteButton.textContent = isMuted ? "Unmute" : "Mute";
        }

        setStatus(isMuted ? "Microphone muted." : "Microphone unmuted.", "warning");
    }

    function initOrderCall() {
        const panel = getElement("order-call-panel");

        if (!panel) {
            return;
        }

        orderId = Number.parseInt(panel.dataset.orderId || "0", 10);

        if (!orderId || orderId <= 0) {
            setStatus("Order ID could not be detected for voice call.", "danger");
            return;
        }

        remoteAudio = getElement("order-call-remote-audio");

        const joinButton = getElement("order-call-join");
        const startButton = getElement("order-call-start");
        const muteButton = getElement("order-call-mute");
        const hangupButton = getElement("order-call-hangup");

        if (joinButton) {
            joinButton.addEventListener("click", joinVoiceRoom);
        }

        if (startButton) {
            startButton.addEventListener("click", startCall);
        }

        if (muteButton) {
            muteButton.addEventListener("click", toggleMute);
        }

        if (hangupButton) {
            hangupButton.addEventListener("click", hangUp);
        }

        setButtons("initial");
        setStatus("Voice room is ready. Join from both customer and caterer browsers.", "warning");
    }

    window.TasteAtDoorOrderCall = {
        initOrderCall
    };

    document.addEventListener("DOMContentLoaded", initOrderCall);
})();
