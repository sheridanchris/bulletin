CREATE TABLE feeds (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    name TEXT NOT NULL,
    url TEXT NOT NULL UNIQUE,
    type TEXT NOT NULL
);

CREATE TABLE entries (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    feed_id INTEGER NOT NULL REFERENCES feeds(id),
    title TEXT NOT NULL,
    description TEXT,
    is_favorited INTEGER NOT NULL CHECK (is_favorited IN (0, 1)),
    url TEXT NOT NULL UNIQUE,
    published_at_timestamp INTEGER NOT NULL,
    updated_at_timestamp INTEGER NOT NULL
);

CREATE VIRTUAL TABLE entries_fts USING fts5 (entry_id, title);

CREATE TRIGGER insert_entries_fts AFTER INSERT ON entries
    BEGIN
        INSERT INTO entries_fts (entry_id, title)
        VALUES (NEW.id, NEW.title);
    END;