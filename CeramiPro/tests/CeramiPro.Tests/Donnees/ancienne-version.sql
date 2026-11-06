-- Atelier fictif enregistré avec l'ancienne version de CeramiPro (schéma à
-- 49 tables, migration « MigrationInitiale »). Sert à vérifier que la
-- simplification du schéma ne perd aucune donnée. Chaque cas délicat de la
-- migration y figure au moins une fois.

-- Comptes : quatre rôles, dont un responsable qui ne doit PAS devenir administrateur.
INSERT INTO "Roles" ("Id", "Code", "Name", "IsSystem", "CreatedAt") VALUES
    (1, 'administrateur', 'Administrateur', TRUE, now()),
    (2, 'responsable', 'Responsable', TRUE, now()),
    (3, 'employe', 'Employé', TRUE, now()),
    (4, 'caissier', 'Caissier', TRUE, now());

INSERT INTO "Users" ("Id", "UserName", "FullName", "PasswordHash", "RoleId", "IsActive",
                     "FailedLoginAttempts", "MustChangePassword", "RefreshToken", "CreatedAt") VALUES
    (1, 'admin', 'Administrateur de l''atelier', 'empreinte-admin', 1, TRUE, 0, FALSE, 'jeton', now()),
    (2, 'karim', 'Karim Responsable', 'empreinte-karim', 2, TRUE, 0, FALSE, NULL, now()),
    (3, 'sara', 'Sara Caissière', 'empreinte-sara', 4, TRUE, 0, FALSE, NULL, now());

INSERT INTO "PaymentMethods" ("Id", "Code", "Name", "RequiresReference", "IsSystem", "IsActive", "CreatedAt") VALUES
    (1, 'especes', 'Espèces', FALSE, TRUE, TRUE, now());

INSERT INTO "BusinessSettings" ("Id", "WorkshopName", "CurrencyCode", "CurrencySymbol", "Culture",
    "TimeZoneId", "DateFormat", "CurrencyDecimals", "DefaultTaxRate", "TaxEnabled",
    "InvoicePrefix", "SalePrefix", "PurchasePrefix", "ProductionPrefix", "CustomOrderPrefix",
    "PaymentPrefix", "FiringPrefix", "InvoiceFooter", "CreatedAt") VALUES
    (1, 'Atelier Tassili', 'DZD', 'DA', 'fr-DZ', 'Africa/Algiers', 'dd/MM/yyyy', 2, 19, FALSE,
     'FAC', 'VTE', 'ACH', 'PROD', 'CMD', 'PAI', 'CUIS', 'Merci et à bientôt.', now());

-- Catalogue : un produit avec code-barres, un produit avec deux variantes en stock.
INSERT INTO "ProductCategories" ("Id", "Name", "IsActive", "CreatedAt") VALUES (1, 'Vases décoratifs', TRUE, now());

INSERT INTO "Products" ("Id", "Reference", "Name", "ProductCategoryId", "ProductionCost", "SellingPrice",
    "CurrentStock", "MinimumStock", "IsCustomizable", "IsActive", "Barcode", "QrCode", "CreatedAt") VALUES
    (1, 'PRD-2026-0001', 'Vase Kabyle', 1, 1200.5, 3500, 10, 2, FALSE, TRUE, '2000000000017', 'QR-1', now()),
    (2, 'PRD-2026-0002', 'Plat Tlemcen', 1, 800, 2500, 3, 1, FALSE, TRUE, NULL, NULL, now());

INSERT INTO "ProductVariants" ("Id", "ProductId", "Reference", "Name", "PriceAdjustment",
    "CurrentStock", "MinimumStock", "IsActive", "CreatedAt") VALUES
    (1, 2, 'PRD-2026-0002-B', 'Bleu', 0, 4, 0, TRUE, now()),
    (2, 2, 'PRD-2026-0002-V', 'Vert', 0, 1.5, 0, TRUE, now());

-- Clients, vente, facture et paiement enregistré sur la facture seulement.
INSERT INTO "Customers" ("Id", "CustomerNumber", "FullName", "PhoneNumber", "IsActive", "CreatedAt") VALUES
    (1, 'CLI-2026-0001', 'Nadia Benali', '0550 00 00 01', TRUE, now());

INSERT INTO "Sales" ("Id", "SaleNumber", "CustomerId", "SaleDate", "Status", "Subtotal", "DiscountAmount",
    "TaxAmount", "TotalAmount", "PaidAmount", "TotalCost", "IsDeleted", "UserId", "CreatedAt") VALUES
    (1, 'VTE-2026-0001', 1, now(), 1, 6000, 0, 0, 6000, 6000, 2000.5, FALSE, 3, now());

INSERT INTO "SaleItems" ("Id", "SaleId", "ProductId", "ProductVariantId", "Description", "Quantity",
    "UnitPrice", "DiscountAmount", "LineTotal", "UnitCost") VALUES
    (1, 1, 1, NULL, 'Vase Kabyle', 1, 3500, 0, 3500, 1200.5),
    (2, 1, 2, 1, 'Plat Tlemcen — Bleu', 1, 2500, 0, 2500, 800);

INSERT INTO "Invoices" ("Id", "InvoiceNumber", "CustomerId", "SaleId", "IssueDate", "Subtotal",
    "DiscountAmount", "TaxRate", "TaxAmount", "TotalAmount", "PaidAmount", "Status", "IsDeleted", "CreatedAt") VALUES
    (1, 'FAC-2026-0001', 1, 1, now(), 6000, 0, 0, 0, 6000, 6000, 2, FALSE, now());

INSERT INTO "Payments" ("Id", "PaymentNumber", "Direction", "InvoiceId", "Amount", "PaymentDate",
    "PaymentMethodId", "IsDeposit", "IsDeleted", "CreatedAt") VALUES
    (1, 'PAI-2026-0001', 0, 1, 6000, now(), 1, FALSE, FALSE, now()),
    -- Décaissement égaré : ne doit jamais compter comme une recette.
    (2, 'PAI-2026-0002', 1, NULL, 900, now(), 1, FALSE, FALSE, now());

-- Commandes personnalisées à des étapes retirées (conception, décoration).
INSERT INTO "CustomOrders" ("Id", "OrderNumber", "CustomerId", "Title", "Quantity", "UnitPrice",
    "DiscountAmount", "TotalAmount", "PaidAmount", "OrderDate", "Deadline", "Status", "IsDeleted",
    "Notes", "CreatedAt") VALUES
    (1, 'CMD-2026-0001', 1, 'Service à thé bleu', 1, 12000, 0, 12000, 4000, now(), now() + interval '10 days',
     1, FALSE, NULL, now()),
    (2, 'CMD-2026-0002', 1, 'Plat mural', 1, 8000, 0, 8000, 0, now(), now() + interval '20 days',
     5, FALSE, 'Bordure dorée', now());

INSERT INTO "Payments" ("Id", "PaymentNumber", "Direction", "CustomOrderId", "CustomerId", "Amount",
    "PaymentDate", "PaymentMethodId", "IsDeposit", "IsDeleted", "CreatedAt") VALUES
    (3, 'PAI-2026-0003', 0, 1, 1, 4000, now(), 1, TRUE, FALSE, now());

-- Matières premières, fournisseurs et achats : modules retirés, à archiver.
INSERT INTO "Units" ("Id", "Code", "Name", "Type", "ConversionFactor", "IsSystem", "IsActive", "CreatedAt") VALUES
    (1, 'kg', 'Kilogramme', 0, 1, TRUE, TRUE, now());
INSERT INTO "MaterialCategories" ("Id", "Name", "IsActive", "CreatedAt") VALUES (1, 'Argile', TRUE, now());
INSERT INTO "Materials" ("Id", "Reference", "Name", "MaterialCategoryId", "UnitId", "CurrentQuantity",
    "MinimumStock", "AverageCost", "LastPurchasePrice", "IsActive", "CreatedAt") VALUES
    (1, 'MAT-2026-0001', 'Argile rouge', 1, 1, 50, 10, 120, 120, TRUE, now());
INSERT INTO "Suppliers" ("Id", "SupplierNumber", "Name", "IsActive", "CreatedAt") VALUES
    (1, 'FRN-2026-0001', 'Carrière de Maghnia', TRUE, now());
INSERT INTO "Purchases" ("Id", "PurchaseNumber", "SupplierId", "PurchaseDate", "Status", "Subtotal",
    "DiscountAmount", "ShippingCost", "TotalAmount", "PaidAmount", "IsDeleted", "CreatedAt") VALUES
    (1, 'ACH-2026-0001', 1, now(), 1, 6000, 0, 0, 6000, 0, FALSE, now());

INSERT INTO "StockAdjustments" ("Id", "Reference", "ItemType", "Reason", "QuantityBefore",
    "CountedQuantity", "Difference", "AdjustmentDate", "CreatedAt") VALUES
    (7, 'AJU-2026-0007', 1, 0, 11, 10, -1, now(), now());

-- Mouvements de stock (ItemType : 0 = matière, 1 = produit, 2 = variante).
INSERT INTO "InventoryTransactions" ("Id", "ItemType", "TransactionType", "MaterialId", "ProductId",
    "ProductVariantId", "PurchaseId", "SaleId", "StockAdjustmentId", "ReversedTransactionId",
    "Quantity", "QuantityBefore", "QuantityAfter", "UnitCost", "TotalCost", "OccurredAt", "Notes", "CreatedAt") VALUES
    -- Achat de matière : quitte la table, reste dans les archives.
    (1, 0, 0, 1, NULL, NULL, 1, NULL, NULL, NULL, 50, 0, 50, 120, 6000, now(), NULL, now()),
    -- Annulation qui inversait ce mouvement de matière.
    (2, 0, 8, 1, NULL, NULL, NULL, NULL, NULL, 1, -50, 50, 0, 120, -6000, now(), NULL, now()),
    -- Entrée de production d'un produit fini : devient une entrée en stock.
    (3, 1, 2, NULL, 1, NULL, NULL, NULL, NULL, NULL, 11, 0, 11, 1200.5, 13205.5, now(), 'Fournée de mai', now()),
    -- Ajustement : garde son type, perd le lien vers l'ajustement (pas une commande !).
    (4, 1, 5, NULL, 1, NULL, NULL, NULL, 7, NULL, -1, 11, 10, 1200.5, -1200.5, now(), NULL, now()),
    -- Transfert : devient une régularisation.
    (5, 1, 7, NULL, 2, NULL, NULL, NULL, NULL, NULL, 3, 0, 3, 800, 2400, now(), NULL, now()),
    -- Vente d'une variante, sans produit renseigné : rattachée au produit.
    (6, 2, 3, NULL, NULL, 1, NULL, 1, NULL, NULL, -1, 5, 4, 800, -800, now(), NULL, now());

-- Une table de module retiré laissée vide : elle doit disparaître, pas s'archiver.
-- (« Kilns » n'est pas rempli.)

-- Les identifiants ont été donnés à la main : les compteurs repartent après
-- la dernière ligne, comme dans une vraie base.
DO $$
DECLARE
    t record;
BEGIN
    FOR t IN
        SELECT table_name FROM information_schema.columns
        WHERE table_schema = 'public' AND column_name = 'Id' AND is_identity = 'YES'
    LOOP
        EXECUTE format(
            'SELECT setval(pg_get_serial_sequence(%L, ''Id''), COALESCE((SELECT MAX("Id") FROM %I), 1), (SELECT COUNT(*) > 0 FROM %I))',
            '"' || t.table_name || '"', t.table_name, t.table_name);
    END LOOP;
END $$;
