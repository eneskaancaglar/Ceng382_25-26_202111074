document.addEventListener("DOMContentLoaded", () => {
    // 1) Yazı değiştirme
    const btnText = document.getElementById("btnText");
    const introText = document.getElementById("introText");

    btnText.addEventListener("click", () => {
        introText.innerHTML = `
      <strong>2. HAFTA: Bu hafta hedef:</strong> Semantik etiketlerle (header, nav, aside, main, footer)
      doğru sayfa iskeleti kurmak. Flexbox ile hizalama, hover efektleri ve kutu modeli
      (padding/margin/border) pratikleri yapacağız.<br/>
    `;
    });

    // 2) Tablo sort
    const table = document.querySelector(".dataTable");
    const tbody = table.querySelector("tbody");

    function sortTableBy(colIndex, type = "text") {
        const rows = Array.from(tbody.querySelectorAll("tr"));
        rows.sort((a, b) => {
            const aVal = a.children[colIndex].textContent.trim();
            const bVal = b.children[colIndex].textContent.trim();
            if (type === "number") return Number(aVal) - Number(bVal);
            return aVal.localeCompare(bVal, "tr", { sensitivity: "base" });
        });

        tbody.innerHTML = "";
        rows.forEach((r) => tbody.appendChild(r));

        Array.from(tbody.querySelectorAll("tr")).forEach((r, i) => {
            r.children[0].textContent = i + 1;
        });
    }

    document.getElementById("btnSortName").addEventListener("click", () => sortTableBy(1, "text"));
    document.getElementById("btnSortVize").addEventListener("click", () => sortTableBy(2, "number"));
    document.getElementById("btnSortFinal").addEventListener("click", () => sortTableBy(3, "number"));

    // 3) Resim değiştirme
    const btnImage = document.getElementById("btnImage");
    const featuredImage = document.getElementById("featuredImage");
    const img1 = "resimler/araba.jpeg";
    const img2 = "resimler/araba2.jpeg";

    btnImage.addEventListener("click", () => {
        const current = featuredImage.getAttribute("src");
        featuredImage.setAttribute("src", current === img1 ? img2 : img1);
    });

    // 4) FX
    const canvas = document.getElementById("fxCanvas");
    if (!canvas) return;

    const prefersReduced =
        window.matchMedia && window.matchMedia("(prefers-reduced-motion: reduce)").matches;

    const ctx = canvas.getContext("2d", { alpha: false });

    const state = {
        w: 0,
        h: 0,
        dpr: Math.min(window.devicePixelRatio || 1, prefersReduced ? 1 : 1.15),
        mouse: { x: 0, y: 0, has: false },
        particles: [],
        last: performance.now(),
        acc: 0,
    };

    const rand = (min, max) => min + Math.random() * (max - min);
    const clamp = (v, a, b) => Math.max(a, Math.min(b, v));

    function resize() {
        const rect = canvas.getBoundingClientRect();
        state.w = Math.max(1, Math.floor(rect.width));
        state.h = Math.max(1, Math.floor(rect.height));

        canvas.width = Math.floor(state.w * state.dpr);
        canvas.height = Math.floor(state.h * state.dpr);
        ctx.setTransform(state.dpr, 0, 0, state.dpr, 0, 0);

        // HER ZAMAN SİYAH
        ctx.fillStyle = "#000";
        ctx.fillRect(0, 0, state.w, state.h);
    }

    function initParticles() {
        const count = prefersReduced ? 80 : 110;
        state.particles = [];

        for (let i = 0; i < count; i++) {
            const hue = rand(38, 48); // altın
            state.particles.push({
                x: rand(0, state.w),
                y: rand(0, state.h),
                px: 0,
                py: 0,
                vx: rand(-1.2, 1.2),
                vy: rand(-1.2, 1.2),
                r: rand(1.6, 2.6),
                hue,
                tx: rand(0, state.w),
                ty: rand(0, state.h),
                // DAHA SIK HEDEF DEĞİŞTİR: daha çok gezsinler
                nextWander: performance.now() + rand(220, 520),
            });
        }
    }

    function bindMouse() {
        const wrap = canvas.parentElement;
        const posFromEvent = (e) => {
            const rect = canvas.getBoundingClientRect();
            return { x: e.clientX - rect.left, y: e.clientY - rect.top };
        };

        wrap.addEventListener("mousemove", (e) => {
            const p = posFromEvent(e);
            state.mouse.x = p.x;
            state.mouse.y = p.y;
            state.mouse.has = true;
        });

        wrap.addEventListener("mouseleave", () => {
            state.mouse.has = false;
        });
    }

    // 30 FPS kilit (kasma yok)
    const TARGET_DT = 1 / 30;

    function loop(now) {
        const dt = Math.min(0.05, (now - state.last) / 1000);
        state.last = now;
        state.acc += dt;

        if (state.acc < TARGET_DT) {
            requestAnimationFrame(loop);
            return;
        }
        state.acc = 0;

        // İZ KALICI OLMASIN: daha fazla sil (alpha büyüt)
        // (0.09 -> 0.30) kuyruk hızlı kaybolur
        ctx.globalCompositeOperation = "source-over";
        ctx.fillStyle = "rgba(0,0,0,0.30)";
        ctx.fillRect(0, 0, state.w, state.h);

        ctx.globalCompositeOperation = "lighter";

        const hasM = state.mouse.has;
        const mx = state.mouse.x;
        const my = state.mouse.y;

        // yakın olan takip
        const followRadius = 180;

        for (const p of state.particles) {
            p.px = p.x;
            p.py = p.y;

            if (!hasM) {
                // 2x hız: wander kuvvetini artır
                if (now > p.nextWander) {
                    p.tx = rand(0, state.w);
                    p.ty = rand(0, state.h);
                    p.nextWander = now + rand(220, 520);
                }
                const dx = p.tx - p.x;
                const dy = p.ty - p.y;
                const d = Math.hypot(dx, dy) + 0.001;

                const ax = (dx / d) * 0.34; // 2x civarı
                const ay = (dy / d) * 0.34;

                p.vx = (p.vx + ax) * 0.90;
                p.vy = (p.vy + ay) * 0.90;
            } else {
                const dxm = mx - p.x;
                const dym = my - p.y;
                const dm = Math.hypot(dxm, dym);

                if (dm < followRadius) {
                    const infl = clamp(1 - dm / followRadius, 0, 1);
                    const inv = 1 / (dm + 0.001);

                    // 2x takip gücü
                    const ax = dxm * inv * (1.35 * infl + 0.12);
                    const ay = dym * inv * (1.35 * infl + 0.12);

                    p.vx = (p.vx + ax) * 0.91;
                    p.vy = (p.vy + ay) * 0.91;
                } else {
                    // uzaktakiler gezsin
                    if (now > p.nextWander) {
                        p.tx = rand(0, state.w);
                        p.ty = rand(0, state.h);
                        p.nextWander = now + rand(220, 600);
                    }
                    const dx = p.tx - p.x;
                    const dy = p.ty - p.y;
                    const d = Math.hypot(dx, dy) + 0.001;

                    const ax = (dx / d) * 0.28;
                    const ay = (dy / d) * 0.28;

                    p.vx = (p.vx + ax) * 0.90;
                    p.vy = (p.vy + ay) * 0.90;
                }
            }

            // HIZ 2x: max hız artır
            const sp = Math.hypot(p.vx, p.vy);
            const maxSp = hasM ? 4.6 : 4.0;
            if (sp > maxSp) {
                const k = maxSp / sp;
                p.vx *= k;
                p.vy *= k;
            }

            p.x += p.vx;
            p.y += p.vy;

            // wrap
            if (p.x < -12) p.x = state.w + 12;
            if (p.x > state.w + 12) p.x = -12;
            if (p.y < -12) p.y = state.h + 12;
            if (p.y > state.h + 12) p.y = -12;

            // altın renk
            const trail = `hsla(${p.hue}, 95%, 60%, 0.45)`;
            const glow = `hsla(${p.hue}, 95%, 62%, 0.22)`;
            const core = `hsla(${p.hue}, 100%, 72%, 0.62)`;

            // İZ İNCE: lineWidth düşür
            ctx.lineCap = "round";
            ctx.lineWidth = 1.1;          // İNCE
            ctx.shadowBlur = 6;           // düşük maliyet
            ctx.shadowColor = `hsla(${p.hue}, 95%, 60%, 0.40)`;
            ctx.strokeStyle = trail;

            ctx.beginPath();
            ctx.moveTo(p.px, p.py);
            ctx.lineTo(p.x, p.y);
            ctx.stroke();

            // top
            ctx.shadowBlur = 10;
            ctx.shadowColor = `hsla(${p.hue}, 95%, 60%, 0.55)`;
            ctx.fillStyle = glow;
            ctx.beginPath();
            ctx.arc(p.x, p.y, p.r, 0, Math.PI * 2);
            ctx.fill();

            // core
            ctx.shadowBlur = 4;
            ctx.shadowColor = `hsla(${p.hue}, 100%, 72%, 0.45)`;
            ctx.fillStyle = core;
            ctx.beginPath();
            ctx.arc(p.x, p.y, Math.max(0.95, p.r * 0.52), 0, Math.PI * 2);
            ctx.fill();
        }

        ctx.globalCompositeOperation = "source-over";
        requestAnimationFrame(loop);
    }

    resize();
    initParticles();
    bindMouse();

    window.addEventListener("resize", () => {
        resize();
        initParticles();
    });

    requestAnimationFrame(loop);
});