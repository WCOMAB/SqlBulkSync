UPDATE TOP (5) company
    SET LastUpdate = GETUTCDATE()
    FROM [SourceDatabase].[dbo].[Company] company
    WHERE Id IN(SELECT TOP 5 Id
                    FROM [SourceDatabase].[dbo].[Company] company
                    ORDER BY NEWID())
