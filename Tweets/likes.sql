CREATE TABLE [dbo].[likes]
(
    [userName] VARCHAR(100) NOT NULL, 
    [messageId] UNIQUEIDENTIFIER NOT NULL FOREIGN KEY REFERENCES [dbo].[messages]([id]), 
    [createDate] DATETIME NULL, 
    PRIMARY KEY ([messageId], [userName])
)
