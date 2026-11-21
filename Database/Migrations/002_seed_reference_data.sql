
INSERT INTO public.categories (id, name)
OVERRIDING SYSTEM VALUE
VALUES
    (1, 'Yemek'),
    (2, 'Ulaşım'),
    (3, 'Kira'),
    (4, 'Fatura'),
    (5, 'Kişisel'),
    (6, 'Yatırım');

SELECT setval(
    pg_get_serial_sequence('public.categories', 'id'),
    (SELECT MAX(id) FROM public.categories)
);

INSERT INTO public.income_categories (id, name)
OVERRIDING SYSTEM VALUE
VALUES
    (1, 'Maaş'),
    (2, 'Freelance'),
    (3, 'Yatırım'),
    (4, 'Ek Gelir'),
    (5, 'Diğer');

SELECT setval(
    pg_get_serial_sequence('public.income_categories', 'id'),
    (SELECT MAX(id) FROM public.income_categories)
);

INSERT INTO public.payment_methods (id, name)
OVERRIDING SYSTEM VALUE
VALUES
    (1, 'Nakit'),
    (2, 'Banka Transferi'),
    (3, 'Kredi Kartı');

SELECT setval(
    pg_get_serial_sequence('public.payment_methods', 'id'),
    (SELECT MAX(id) FROM public.payment_methods)
);
