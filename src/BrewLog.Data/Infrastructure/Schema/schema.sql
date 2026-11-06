CREATE TABLE IF NOT EXISTS users (
  id INTEGER PRIMARY KEY AUTOINCREMENT,
  login TEXT NOT NULL UNIQUE,
  full_name TEXT NOT NULL,
  role TEXT NOT NULL CHECK (role IN ('Brewer','Assistant','Owner')),
  password_hash TEXT NOT NULL,
  is_active INTEGER NOT NULL DEFAULT 1,
  remember_token_hash TEXT NULL,
  created_at TEXT NOT NULL
);

CREATE TABLE IF NOT EXISTS styles (
  id INTEGER PRIMARY KEY AUTOINCREMENT,
  code TEXT NOT NULL UNIQUE,            -- '21A'
  name TEXT NOT NULL,                   -- 'American IPA'
  category TEXT NOT NULL,
  og_min REAL NOT NULL, og_max REAL NOT NULL,
  fg_min REAL NOT NULL, fg_max REAL NOT NULL,
  abv_min REAL NOT NULL, abv_max REAL NOT NULL,
  ibu_min REAL NOT NULL, ibu_max REAL NOT NULL,
  srm_min REAL NOT NULL, srm_max REAL NOT NULL
);

CREATE TABLE IF NOT EXISTS ingredient_catalog (
  id INTEGER PRIMARY KEY AUTOINCREMENT,
  kind TEXT NOT NULL CHECK (kind IN ('Malt','Hop','Yeast','Adjunct')),
  name TEXT NOT NULL,
  yield_percent REAL NULL,              -- Malt, Adjunct
  color_lovibond REAL NULL,             -- Malt
  is_mashed INTEGER NULL,               -- Malt: 1 — зерно (з ефективністю), 0 — екстракт
  alpha_percent REAL NULL,              -- Hop
  attenuation_percent REAL NULL,        -- Yeast
  temp_min_c REAL NULL, temp_max_c REAL NULL,  -- Yeast
  min_stock REAL NOT NULL DEFAULT 0,    -- в одиницях складу (кг / г / пак.)
  last_price REAL NOT NULL DEFAULT 0,   -- грн за одиницю
  notes TEXT NULL,
  UNIQUE (kind, name)
);

CREATE TABLE IF NOT EXISTS recipes (
  id INTEGER PRIMARY KEY AUTOINCREMENT,
  name TEXT NOT NULL,
  style_id INTEGER NULL REFERENCES styles(id),
  batch_volume_l REAL NOT NULL,
  efficiency_percent REAL NOT NULL DEFAULT 72,
  boil_time_min INTEGER NOT NULL DEFAULT 60,
  ibu_formula TEXT NOT NULL DEFAULT 'Tinseth' CHECK (ibu_formula IN ('Tinseth','Rager')),
  notes TEXT NULL,
  author_id INTEGER NULL REFERENCES users(id),
  created_at TEXT NOT NULL,
  updated_at TEXT NOT NULL
);

CREATE TABLE IF NOT EXISTS recipe_ingredients (
  id INTEGER PRIMARY KEY AUTOINCREMENT,
  recipe_id INTEGER NOT NULL REFERENCES recipes(id) ON DELETE CASCADE,
  catalog_id INTEGER NOT NULL REFERENCES ingredient_catalog(id),
  kind TEXT NOT NULL CHECK (kind IN ('Malt','Hop','Yeast','Adjunct')),
  name TEXT NOT NULL,
  amount REAL NOT NULL,                 -- кг (Malt, Adjunct), г (Hop), пак. (Yeast)
  yield_percent REAL NULL, color_lovibond REAL NULL, is_mashed INTEGER NULL,
  alpha_percent REAL NULL, hop_time_min REAL NULL,
  hop_use TEXT NULL CHECK (hop_use IN ('Boil','DryHop')),
  attenuation_percent REAL NULL, temp_min_c REAL NULL, temp_max_c REAL NULL,
  sort_order INTEGER NOT NULL DEFAULT 0
);

CREATE TABLE IF NOT EXISTS suppliers (
  id INTEGER PRIMARY KEY AUTOINCREMENT,
  name TEXT NOT NULL UNIQUE,
  phone TEXT NULL,
  email TEXT NULL
);

CREATE TABLE IF NOT EXISTS stock_lots (
  id INTEGER PRIMARY KEY AUTOINCREMENT,
  catalog_id INTEGER NOT NULL REFERENCES ingredient_catalog(id),
  supplier_id INTEGER NULL REFERENCES suppliers(id),
  quantity_received REAL NOT NULL,
  quantity_left REAL NOT NULL,
  unit_price REAL NOT NULL,
  received_at TEXT NOT NULL,
  expires_at TEXT NULL,
  lot_code TEXT NULL
);

CREATE TABLE IF NOT EXISTS batches (
  id INTEGER PRIMARY KEY AUTOINCREMENT,
  number TEXT NOT NULL UNIQUE,          -- BL-2026-001
  recipe_id INTEGER NOT NULL REFERENCES recipes(id),
  recipe_name TEXT NOT NULL,
  style_name TEXT NULL,                 -- знімок: етикетка не потребує ViewRecipes
  volume_l REAL NOT NULL,
  state TEXT NOT NULL CHECK (state IN ('Planned','Brewing','Fermenting','Conditioning','Packaged')),
  planned_date TEXT NOT NULL,
  brewed_at TEXT NULL, fermenting_at TEXT NULL, conditioning_at TEXT NULL, packaged_at TEXT NULL,
  planned_og REAL NOT NULL, planned_fg REAL NOT NULL, planned_abv REAL NOT NULL, planned_srm REAL NOT NULL,
  temp_min_c REAL NULL, temp_max_c REAL NULL,
  actual_og REAL NULL,
  ingredients_cost REAL NULL,           -- грн, сума списаної сировини
  assistant_id INTEGER NULL REFERENCES users(id),
  created_by INTEGER NULL REFERENCES users(id),
  notes TEXT NULL
);

CREATE TABLE IF NOT EXISTS stock_movements (
  id INTEGER PRIMARY KEY AUTOINCREMENT,
  lot_id INTEGER NOT NULL REFERENCES stock_lots(id),
  batch_id INTEGER NULL REFERENCES batches(id),
  kind TEXT NOT NULL CHECK (kind IN ('Receipt','WriteOff','Adjustment')),
  quantity REAL NOT NULL,               -- > 0 прихід, < 0 списання
  unit_price REAL NOT NULL,
  created_at TEXT NOT NULL,
  user_id INTEGER NULL REFERENCES users(id)
);

CREATE TABLE IF NOT EXISTS measurements (
  id INTEGER PRIMARY KEY AUTOINCREMENT,
  batch_id INTEGER NOT NULL REFERENCES batches(id) ON DELETE CASCADE,
  measured_at TEXT NOT NULL,
  gravity REAL NULL,
  temperature_c REAL NULL,
  note TEXT NULL,
  user_id INTEGER NULL REFERENCES users(id)
);

CREATE TABLE IF NOT EXISTS packagings (
  id INTEGER PRIMARY KEY AUTOINCREMENT,
  batch_id INTEGER NOT NULL UNIQUE REFERENCES batches(id) ON DELETE CASCADE,
  packaged_at TEXT NOT NULL,
  bottles_count INTEGER NOT NULL DEFAULT 0,
  bottle_volume_l REAL NOT NULL DEFAULT 0.5,
  kegs_count INTEGER NOT NULL DEFAULT 0,
  keg_volume_l REAL NOT NULL DEFAULT 20,
  final_gravity REAL NULL,
  loss_l REAL NOT NULL DEFAULT 0,
  user_id INTEGER NULL REFERENCES users(id),
  notes TEXT NULL
);

CREATE INDEX IF NOT EXISTS idx_measurements_batch_id ON measurements(batch_id);
CREATE INDEX IF NOT EXISTS idx_stock_lots_catalog_id ON stock_lots(catalog_id);
CREATE INDEX IF NOT EXISTS idx_recipe_ingredients_recipe_id ON recipe_ingredients(recipe_id);
