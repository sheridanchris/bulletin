PRAGMA foreign_keys = ON;

CREATE TABLE IF NOT EXISTS users(
     Id INTEGER PRIMARY KEY AUTOINCREMENT,
     Username TEXT
);

CREATE TABLE IF NOT EXISTS posts(
     Id INTEGER PRIMARY KEY AUTOINCREMENT,
     Headline TEXT NOT NULL,
     Description TEXT,
     Link TEXT NOT NULL,
     Poster INTEGER,
     PublishedDate TEXT,
     FOREIGN KEY(Poster) REFERENCES users(Id) 
);

CREATE TABLE IF NOT EXISTS comments(
     Id INTEGER PRIMARY KEY AUTOINCREMENT,
     PostId INTEGER NOT NULL,
     UserId INTEGER NOT NULL,
     ParentId INTEGER,
     FOREIGN KEY (PostId) REFERENCES posts(Id),
     FOREIGN KEY (UserId) REFERENCES users(Id),
     FOREIGN KEY (ParentId) REFERENCES comments(Id)
);

CREATE TABLE IF NOT EXISTS post_votes(
     PostId INTEGER,
     UserId INTEGER,
     Type INTEGER CHECK (Type IN (1, 2)),
     PRIMARY KEY (PostId, UserId),
     FOREIGN KEY (PostId) REFERENCES posts(Id),
     FOREIGN KEY (UserId) REFERENCES users(Id)
);

CREATE TABLE IF NOT EXISTS comment_votes(
     CommentId INTEGER,
     UserId INTEGER,
     Type INTEGER CHECK (Type IN (1, 2)),
     PRIMARY KEY (CommentId, UserId),
     FOREIGN KEY (CommentId) REFERENCES comments(Id),
     FOREIGN KEY (UserId) REFERENCES users(Id)
);