CREATE TABLE "Users"(
     "Id" UUID PRIMARY KEY,
     "Username" TEXT
);

CREATE TABLE "Posts"(
    "Id" UUID PRIMARY KEY,
    "Headline" TEXT NOT NULL,
    "Link" TEXT NOT NULL,
    "PosterId" UUID REFERENCES "Users"("Id"),
    "PublishedDate" TIMESTAMP NOT NULL,
    "Score" INT NOT NULL
);

CREATE UNIQUE INDEX idx_link ON "Posts"("Link");

CREATE TABLE "Comments"(
    "Id" UUID PRIMARY KEY,
    "Comment" TEXT NOT NULL,
    "PostId" UUID NOT NULL REFERENCES "Posts"("Id"),
    "UserId" UUID NOT NULL REFERENCES "Users"("Id"),
    "ParentId" UUID REFERENCES "Comments"("Id")
);

CREATE TYPE "VoteType" AS ENUM('Positive', 'Negative');

CREATE TABLE "PostVotes"(
    "PostId" UUID NOT NULL REFERENCES "Posts"("Id"),
    "VoterId" UUID NOT NULL REFERENCES "Users"("Id"),
    "Type" "VoteType" NOT NULL,
    PRIMARY KEY ("PostId", "VoterId")
);

CREATE TABLE "CommentVotes"(
    "CommentId" UUID NOT NULL REFERENCES "Comments"("Id"),
    "VoterId" UUID NOT NULL REFERENCES "Users"("Id"),
    "Type" "VoteType" NOT NULL,
    PRIMARY KEY ("CommentId", "VoterId")
);