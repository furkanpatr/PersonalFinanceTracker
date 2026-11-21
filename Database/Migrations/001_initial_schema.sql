CREATE TABLE public.users (
    id INTEGER GENERATED ALWAYS AS IDENTITY,
    name VARCHAR(100) NOT NULL,
    surname VARCHAR(100) NOT NULL,
    phone VARCHAR(100),
    email VARCHAR(100) NOT NULL,
    password_hash VARCHAR(100) NOT NULL,

    CONSTRAINT users_pkey PRIMARY KEY (id),
    CONSTRAINT unique_email UNIQUE (email)
);

CREATE TABLE public.categories (
    id INTEGER GENERATED ALWAYS AS IDENTITY,
    name VARCHAR(100) NOT NULL,

    CONSTRAINT categories_pkey PRIMARY KEY (id),
    CONSTRAINT categories_name_key UNIQUE (name)
);

CREATE TABLE public.payment_methods (
    id INTEGER GENERATED ALWAYS AS IDENTITY,
    name VARCHAR(100) NOT NULL,

    CONSTRAINT payment_methods_pkey PRIMARY KEY (id),
    CONSTRAINT payment_methods_name_key UNIQUE (name)
);

CREATE TABLE public.income_categories (
    id INTEGER GENERATED ALWAYS AS IDENTITY,
    name VARCHAR(100) NOT NULL,

    CONSTRAINT income_categories_pkey PRIMARY KEY (id),
    CONSTRAINT income_categories_name_key UNIQUE (name)
);

CREATE TABLE public.expenses (
    id INTEGER GENERATED ALWAYS AS IDENTITY,
    user_id INTEGER NOT NULL,
    category_id INTEGER NOT NULL,
    amount NUMERIC(10,2) NOT NULL,
    date DATE NOT NULL,
    place VARCHAR(100),
    description VARCHAR(100),
    payment_method_id INTEGER NOT NULL,

    CONSTRAINT expenses_pkey PRIMARY KEY (id),
    CONSTRAINT expenses_amount_check CHECK (amount > 0),

    CONSTRAINT expenses_user_id_fkey
        FOREIGN KEY (user_id) REFERENCES public.users(id),

    CONSTRAINT expenses_category_id_fkey
        FOREIGN KEY (category_id) REFERENCES public.categories(id),

    CONSTRAINT expenses_payment_method_id_fkey
        FOREIGN KEY (payment_method_id) REFERENCES public.payment_methods(id)
);

CREATE TABLE public.incomes (
    id INTEGER GENERATED ALWAYS AS IDENTITY,
    user_id INTEGER NOT NULL,
    income_category_id INTEGER NOT NULL,
    amount NUMERIC(10,2) NOT NULL,
    date DATE NOT NULL,
    description VARCHAR(200),

    CONSTRAINT incomes_pkey PRIMARY KEY (id),

    CONSTRAINT incomes_user_id_fkey
        FOREIGN KEY (user_id) REFERENCES public.users(id),

    CONSTRAINT incomes_income_category_id_fkey
        FOREIGN KEY (income_category_id) REFERENCES public.income_categories(id)
);

CREATE TABLE public.budget_limits (
    id INTEGER GENERATED ALWAYS AS IDENTITY,
    user_id INTEGER NOT NULL,
    category_id INTEGER NOT NULL,
    amount NUMERIC(10,2) NOT NULL,
    period DATE NOT NULL,

    CONSTRAINT budget_limits_pkey PRIMARY KEY (id),

    CONSTRAINT budget_limits_amount_check
        CHECK (amount > 0),

    CONSTRAINT chk_budget_period_first_day
        CHECK (EXTRACT(DAY FROM period) = 1),

    CONSTRAINT budget_limits_user_id_category_id_period_key
        UNIQUE (user_id, category_id, period),

    CONSTRAINT budget_limits_user_id_fkey
        FOREIGN KEY (user_id) REFERENCES public.users(id),

    CONSTRAINT budget_limits_category_id_fkey
        FOREIGN KEY (category_id) REFERENCES public.categories(id)
);

CREATE TABLE public.recurring_payments (
    id INTEGER GENERATED ALWAYS AS IDENTITY,
    user_id INTEGER NOT NULL,
    category_id INTEGER NOT NULL,
    payment_method_id INTEGER NOT NULL,
    name VARCHAR(100) NOT NULL,
    amount NUMERIC(10,2) NOT NULL,
    frequency VARCHAR(20) NOT NULL,
    next_due_date DATE NOT NULL,
    importance_level VARCHAR(20) NOT NULL,
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    description VARCHAR(250),

    CONSTRAINT recurring_payments_pkey PRIMARY KEY (id),

    CONSTRAINT recurring_payments_amount_check
        CHECK (amount > 0),

    CONSTRAINT chk_recurring_frequency
        CHECK (frequency IN ('weekly', 'monthly', 'yearly')),

    CONSTRAINT chk_recurring_importance
        CHECK (importance_level IN ('low', 'medium', 'high')),

    CONSTRAINT recurring_payments_user_id_fkey
        FOREIGN KEY (user_id) REFERENCES public.users(id),

    CONSTRAINT recurring_payments_category_id_fkey
        FOREIGN KEY (category_id) REFERENCES public.categories(id),

    CONSTRAINT recurring_payments_payment_method_id_fkey
        FOREIGN KEY (payment_method_id) REFERENCES public.payment_methods(id)
);

CREATE TABLE public.future_payments (
    id INTEGER GENERATED ALWAYS AS IDENTITY,
    user_id INTEGER,
    category_id INTEGER,
    amount NUMERIC(10,2) NOT NULL,
    planned_date DATE,
    status VARCHAR(100) NOT NULL DEFAULT 'pending',
    importance_level VARCHAR(100) NOT NULL,
    payment_method_id INTEGER,
    recurring_payment_id INTEGER,
    original_due_date DATE,

    CONSTRAINT future_payments_pkey PRIMARY KEY (id),

    CONSTRAINT future_payments_amount_check
        CHECK (amount > 0),

    CONSTRAINT chk_future_payment_status
        CHECK (status IN ('pending', 'paid')),

    CONSTRAINT chk_future_payment_importance
        CHECK (importance_level IN ('low', 'medium', 'high')),

    CONSTRAINT future_payments_user_id_fkey
        FOREIGN KEY (user_id) REFERENCES public.users(id),

    CONSTRAINT future_payments_category_id_fkey
        FOREIGN KEY (category_id) REFERENCES public.categories(id),

    CONSTRAINT future_payments_payment_method_id_fkey
        FOREIGN KEY (payment_method_id) REFERENCES public.payment_methods(id),

    CONSTRAINT future_payments_recurring_payment_id_fkey
        FOREIGN KEY (recurring_payment_id)
        REFERENCES public.recurring_payments(id)
        ON DELETE SET NULL
);

CREATE UNIQUE INDEX ux_future_payments_recurring_original_due_date
ON public.future_payments (recurring_payment_id, original_due_date)
WHERE recurring_payment_id IS NOT NULL;

CREATE TABLE public.future_payment_history (
    id INTEGER GENERATED ALWAYS AS IDENTITY,
    future_payment_id INTEGER,
    user_id INTEGER NOT NULL,
    action VARCHAR(20) NOT NULL,
    old_planned_date DATE NOT NULL,
    new_planned_date DATE,
    importance_level VARCHAR(20) NOT NULL,
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,

    CONSTRAINT future_payment_history_pkey PRIMARY KEY (id),

    CONSTRAINT future_payment_history_action_check
        CHECK (action IN ('postponed', 'paid')),

    CONSTRAINT future_payment_history_importance_level_check
        CHECK (importance_level IN ('low', 'medium', 'high')),

    CONSTRAINT chk_future_payment_history_action_dates
        CHECK (
            (action = 'postponed'
                AND new_planned_date IS NOT NULL
                AND new_planned_date > old_planned_date)
            OR
            (action = 'paid'
                AND new_planned_date IS NULL)
        ),

    CONSTRAINT future_payment_history_future_payment_id_fkey
        FOREIGN KEY (future_payment_id)
        REFERENCES public.future_payments(id)
        ON DELETE SET NULL,

    CONSTRAINT future_payment_history_user_id_fkey
        FOREIGN KEY (user_id)
        REFERENCES public.users(id)
);
