USE Northwind;
GO

DROP TABLE IF EXISTS ShippersContactInfo;
GO

CREATE TABLE ShippersContactInfo (
    ContactInfoID INT IDENTITY(1,1) PRIMARY KEY,
    ShipperID INT NOT NULL,
    Email NVARCHAR(100) NOT NULL,
    Website NVARCHAR(150) NULL,
    Phone NVARCHAR(30) NULL,
    City NVARCHAR(100) NULL,
    Country NVARCHAR(100) NULL,
    Postcode NVARCHAR(20) NULL,
    Address NVARCHAR(200) NULL,
    CONSTRAINT FK_ShippersContactInfo_Shippers
        FOREIGN KEY (ShipperID) REFERENCES Shippers(ShipperID)
);
GO

USE Northwind;
GO

INSERT INTO ShippersContactInfo
(ShipperID, Email, Website, Phone, City, Country, Postcode, Address)
VALUES
(1, 'speedy@shipping.com', 'https://www.speedyexpress.com', '(503) 555-9831', 'Portland', 'USA', '97201', '101 Speedy Street'),
(2, 'contact@unitedpackage.com', 'https://www.unitedpackage.com', '(503) 555-3199', 'Seattle', 'USA', '98101', '202 United Avenue'),
(3, 'support@federalshipping.com', 'https://www.federalshipping.com', '(503) 555-9931', 'New York', 'USA', '10001', '303 Federal Road');
GO
