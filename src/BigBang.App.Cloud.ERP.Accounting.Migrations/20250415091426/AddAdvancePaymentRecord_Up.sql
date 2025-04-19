--SQL UP SCRIPT

IF NOT EXISTS(SELECT * FROM [VoucherTemplate] WHERE [Name]='AdvancePayment')
BEGIN
	INSERT [dbo].[VoucherTemplate] ([Id], [Name], [DisplayName], [TitleFormat]) VALUES (9, N'AdvancePayment', N'پیش پرداخت', N'پیش پرداخت به #FirstName #LastName')
END