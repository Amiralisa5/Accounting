IF NOT EXISTS(
    SELECT * FROM [dbo].[VoucherTemplate] WHERE [Name] = 'AdvanceReceipt'
)
BEGIN
    INSERT INTO [dbo].[VoucherTemplate] ([Id], [Name], [DisplayName], [TitleFormat])
    VALUES (8, 'AdvanceReceipt', N' پیش دریافت ', N'پیش دریافت از  #FirstName# #LastName#')
END
GO
--SQL UP SCRIPT