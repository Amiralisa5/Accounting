IF NOT EXISTS(SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME='VoucherTemplate')
BEGIN
	CREATE TABLE [dbo].[VoucherTemplate](
		[Id] [tinyint] NOT NULL,
		[Name] [VARCHAR](32) NOT NULL,
		[DisplayName] [NVARCHAR](32) NOT NULL,
		[TitleFormat] [NVARCHAR](64) NOT NULL,
		CONSTRAINT [PK_VoucherTemplate] PRIMARY KEY CLUSTERED 
	(
		[Id] ASC
	)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
	) ON [PRIMARY]

	CREATE UNIQUE NONCLUSTERED INDEX [IDX_VoucherTemplate_Name] ON [dbo].[VoucherTemplate]
	(
		[Name] ASC
	)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON)
END
GO

IF NOT EXISTS(SELECT * FROM [VoucherTemplate] WHERE [Name]='Custom')
BEGIN
	INSERT INTO [dbo].[VoucherTemplate] ([Id], [Name],[DisplayName],[TitleFormat]) VALUES (0, 'Custom',N'دستی', N'سند حسابداری')
END

IF NOT EXISTS(SELECT * FROM [VoucherTemplate] WHERE [Name]='Expenses')
BEGIN
	INSERT INTO [dbo].[VoucherTemplate] ([Id], [Name],[DisplayName],[TitleFormat]) VALUES (1, 'Expenses',N'مخارج شخصی', N'برداشت #AmountInCurrency#')
END

IF NOT EXISTS(SELECT * FROM [VoucherTemplate] WHERE [Name]='ProductBuy')
BEGIN
	INSERT [dbo].[VoucherTemplate] ([Id], [Name], [DisplayName], [TitleFormat]) VALUES (2, N'ProductBuy', N'خرید کالا', N'بابت خرید #GoodsName#')
END

IF NOT EXISTS(SELECT * FROM [VoucherTemplate] WHERE [Name]='ProductSell')
BEGIN
	INSERT [dbo].[VoucherTemplate] ([Id], [Name], [DisplayName], [TitleFormat]) VALUES (3, N'ProductSell', N'فروش', N'فروش کالا #GoodsName#')
END

IF NOT EXISTS(SELECT * FROM [VoucherTemplate] WHERE [Name]='Cost')
BEGIN
	INSERT [dbo].[VoucherTemplate] ([Id], [Name], [DisplayName], [TitleFormat]) VALUES (4, N'Cost', N'هزینه', N'هزینه بابت #Holder#')
END

IF NOT EXISTS(SELECT * FROM [VoucherTemplate] WHERE [Name]='PayDebt')
BEGIN
	INSERT [dbo].[VoucherTemplate] ([Id], [Name], [DisplayName], [TitleFormat]) VALUES (5, N'PayDebt', N'پرداخت بدهی', N'بدهی به #FirstName# #LastName#')
END

IF NOT EXISTS(SELECT * FROM [VoucherTemplate] WHERE [Name]='Deposit')
BEGIN
	INSERT [dbo].[VoucherTemplate] ([Id], [Name], [DisplayName], [TitleFormat]) VALUES (6, N'Deposit', N'واریز به بانک', N'واریز به #Bank# - #CardNo#')
END

IF NOT EXISTS(SELECT * FROM [VoucherTemplate] WHERE [Name]='ReceiveDebt')
BEGIN
	INSERT [dbo].[VoucherTemplate] ([Id], [Name], [DisplayName], [TitleFormat]) VALUES (7, N'ReceiveDebt', N'دریافت طلب', N'طلب از #FirstName# #LastName#')
END

IF NOT EXISTS(SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME='Owner')
BEGIN
	CREATE TABLE [dbo].[Owner](
		[Id] [uniqueidentifier] NOT NULL,
		[FirstName] [nvarchar](50) NOT NULL,
		[LastName] [nvarchar](50) NOT NULL,
		[MobileNumber] [char](11) NOT NULL,
		[UserId] [bigint]  NOT NULL,
		CONSTRAINT [PK_Owner] PRIMARY KEY CLUSTERED 
	(
		[Id] ASC
	)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
	) ON [PRIMARY]

	CREATE UNIQUE NONCLUSTERED INDEX [IDX_Unique_UserId] ON [dbo].[Owner]
	(
		[UserId] ASC
	)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON)
END
GO

IF NOT EXISTS(SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME='Business')
BEGIN
	CREATE TABLE [dbo].[Business](
		[Id] [uniqueidentifier] NOT NULL,
		[Name] [nvarchar](100) NOT NULL,
		[OwnerId] [uniqueidentifier] NOT NULL,
		[PodBusinessId] [bigint]  NOT NULL,
		CONSTRAINT [PK_Business] PRIMARY KEY CLUSTERED 
	(
		[Id] ASC
	)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
	) ON [PRIMARY]

	ALTER TABLE [dbo].[Business] WITH CHECK ADD CONSTRAINT [FK_Business_Owner] FOREIGN KEY([OwnerId])
	REFERENCES [dbo].[Owner] ([Id])
	ALTER TABLE [dbo].[Business] CHECK CONSTRAINT [FK_Business_Owner]
END

IF NOT EXISTS(SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME='FiscalPeriod')
BEGIN
	CREATE TABLE [dbo].[FiscalPeriod](
		[Id] [uniqueidentifier] NOT NULL,
		[Title] [nvarchar](50) NOT NULL,
		[FromDate] [date] NOT NULL,
		[ToDate] [date] NOT NULL,
		[Status] [tinyint] NOT NULL,
		[BusinessId] [uniqueidentifier] NOT NULL,
		CONSTRAINT [PK_FiscalPeriod] PRIMARY KEY CLUSTERED 
	(
		[Id] ASC
	)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
	) ON [PRIMARY]

	ALTER TABLE [dbo].[FiscalPeriod] WITH CHECK ADD CONSTRAINT [FK_FiscalPeriod_Business] FOREIGN KEY([BusinessId])
	REFERENCES [dbo].[Business] ([Id])
	ALTER TABLE [dbo].[FiscalPeriod] CHECK CONSTRAINT [FK_FiscalPeriod_Business]
END
GO

IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME='Account')
BEGIN
	CREATE TABLE [dbo].[Account](
		[Id] [uniqueidentifier] NOT NULL,
		[DisplayName] [nvarchar](50) NOT NULL,
		[Level] [tinyint] NOT NULL,
		[Code] [varchar](5) NULL,
		[IsPermanent] [bit] NULL,
		[IsCustom] [bit] NOT NULL,
		[Nature] [tinyint] NULL,
		[Name] [varchar](50) NOT NULL,
		[LookupType] [tinyint] NULL,
		[ParentAccountId] [uniqueidentifier] NULL,
		[FiscalPeriodId] [uniqueidentifier] NOT NULL,
		CONSTRAINT [PK_Account] PRIMARY KEY CLUSTERED 
	(
		[Id] ASC
	)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
	) ON [PRIMARY]

	ALTER TABLE [dbo].[Account] WITH CHECK ADD CONSTRAINT [FK_Account_Account] FOREIGN KEY([ParentAccountId])
	REFERENCES [dbo].[Account] ([Id])
	ALTER TABLE [dbo].[Account] CHECK CONSTRAINT [FK_Account_Account]

	ALTER TABLE [dbo].[Account] WITH CHECK ADD CONSTRAINT [FK_Account_FiscalPeriod] FOREIGN KEY([FiscalPeriodId])
	REFERENCES [dbo].[FiscalPeriod] ([Id])
	ALTER TABLE [dbo].[Account] CHECK CONSTRAINT [FK_Account_FiscalPeriod]
END
GO

IF NOT EXISTS(SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME='PersonRole')
BEGIN
	CREATE TABLE [dbo].[PersonRole](
		[Id] [UNIQUEIDENTIFIER] NOT NULL,
		[AccountId] [UNIQUEIDENTIFIER] NOT NULL,
		[PersonRoleTypeId] [tinyint] NOT NULL,
	 CONSTRAINT [PK_PersonRole] PRIMARY KEY CLUSTERED 
	(
		[Id] ASC
	)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
	) ON [PRIMARY]

	ALTER TABLE [dbo].[PersonRole]  WITH CHECK ADD  CONSTRAINT [FK_PersonRole_Account] FOREIGN KEY([AccountId])
	REFERENCES [dbo].[Account] ([Id]) ON DELETE CASCADE
	ALTER TABLE [dbo].[PersonRole] CHECK CONSTRAINT [FK_PersonRole_Account]
END
GO

IF NOT EXISTS(SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME='Voucher')
BEGIN
	CREATE TABLE [dbo].[Voucher](
		[Id] [uniqueidentifier] NOT NULL,
		[FiscalPeriodId] [uniqueidentifier] NOT NULL,
		[VoucherTemplateId] [tinyint] NOT NULL,
		[Number] [varchar](16) NOT NULL,
		[Description] [nvarchar](255) NULL,
		[CreationDate] [datetime] NOT NULL,
		[EffectiveDate] [datetime] NOT NULL,
		[Type] [tinyint] NOT NULL,
		[Title] [nvarchar](128) NOT NULL,
		[Amount] [bigint] NOT NULL
		CONSTRAINT [PK_Voucher] PRIMARY KEY CLUSTERED 
	(
		[Id] ASC
	)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
	) ON [PRIMARY]

	ALTER TABLE [dbo].[Voucher] WITH CHECK ADD CONSTRAINT [FK_Voucher_FiscalPeriod] FOREIGN KEY([FiscalPeriodId])
	REFERENCES [dbo].[FiscalPeriod] ([Id])
	ALTER TABLE [dbo].[Voucher] CHECK CONSTRAINT [FK_Voucher_FiscalPeriod]

	ALTER TABLE [dbo].[Voucher]  WITH CHECK ADD  CONSTRAINT [FK_Voucher_VoucherTemplate] FOREIGN KEY([VoucherTemplateId])
	REFERENCES [dbo].[VoucherTemplate] ([Id])
	ALTER TABLE [dbo].[Voucher] CHECK CONSTRAINT [FK_Voucher_VoucherTemplate]
END
GO

IF NOT EXISTS(SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME='Article')
BEGIN
	CREATE TABLE [dbo].[Article](
		[Id] [uniqueidentifier] NOT NULL,
		[VoucherId] [uniqueidentifier] NOT NULL,
		[AccountId] [uniqueidentifier] NOT NULL,
		[LookupId] [uniqueidentifier] NULL,
		[Quantity] [int] NULL,
		[Fee] [bigint] NULL,
		[Amount] [bigint] NOT NULL,
		[Currency] [tinyint] NOT NULL,
		[Type] [tinyint] NOT NULL,
		[IsTransactionalOnly] [bit] NOT NULL,
		CONSTRAINT [PK_Article] PRIMARY KEY CLUSTERED
	(
		[Id] ASC
	)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
	) ON [PRIMARY]

	ALTER TABLE [dbo].[Article]  WITH CHECK ADD  CONSTRAINT [FK_Article_Voucher] FOREIGN KEY([VoucherId])
	REFERENCES [dbo].[Voucher] ([Id]) ON DELETE CASCADE
	ALTER TABLE [dbo].[Article] CHECK CONSTRAINT [FK_Article_Voucher]

	ALTER TABLE [dbo].[Article] WITH CHECK ADD CONSTRAINT [FK_Article_Account] FOREIGN KEY([AccountId])
	REFERENCES [dbo].[Account] ([Id])
	ALTER TABLE [dbo].[Article] CHECK CONSTRAINT [FK_Article_Account]
END
GO

IF NOT EXISTS(SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME='PersonAccount')
BEGIN
	CREATE TABLE [dbo].[PersonAccount](
		[Id] [uniqueidentifier] NOT NULL,
		[FirstName] [nvarchar](50) NOT NULL,
		[LastName] [nvarchar](50) NOT NULL,
		[MobileNumber] [char](11) NULL,
		[BusinessId] [uniqueidentifier] NOT NULL,
		CONSTRAINT [PK_Person] PRIMARY KEY CLUSTERED
	(
		[Id] ASC
	)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
	) ON [PRIMARY]

	ALTER TABLE [dbo].[PersonAccount] WITH CHECK ADD CONSTRAINT [FK_PersonAccount_Business] FOREIGN KEY([BusinessId])
	REFERENCES [dbo].[Business] ([Id])
	ALTER TABLE [dbo].[PersonAccount] CHECK CONSTRAINT [FK_PersonAccount_Business]
END
GO

IF NOT EXISTS(SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME='PersonAccountRole')
BEGIN
	CREATE TABLE [dbo].[PersonAccountRole](
		[Id] [UNIQUEIDENTIFIER] NOT NULL,
		[PersonAccountId] [UNIQUEIDENTIFIER] NOT NULL,
		[PersonRoleTypeId] [tinyint] NOT NULL,
	 CONSTRAINT [PK_PersonAccountRole] PRIMARY KEY CLUSTERED 
	(
		[Id] ASC
	)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
	) ON [PRIMARY]

	ALTER TABLE [dbo].[PersonAccountRole]  WITH CHECK ADD  CONSTRAINT [FK_PersonAccountRole_PersonAccount] FOREIGN KEY([PersonAccountId])
	REFERENCES [dbo].[PersonAccount] ([Id]) ON DELETE CASCADE
	ALTER TABLE [dbo].[PersonAccountRole] CHECK CONSTRAINT [FK_PersonAccountRole_PersonAccount]
END
GO

IF NOT EXISTS(SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME='BankAccount')
BEGIN
	CREATE TABLE [dbo].[BankAccount](
		[Id] [uniqueidentifier] NOT NULL,
		[Bank] [tinyint] NOT NULL,
		[HolderName] [nvarchar](100) NOT NULL,
		[ShebaNumber] [char](24) NULL,
		[CardNumber] [char](16) NOT NULL,
		[Balance] [bigint] NOT NULL,
		[BusinessId] [uniqueidentifier] NOT NULL,
		CONSTRAINT [PK_BankAccount] PRIMARY KEY CLUSTERED
	(
		[Id] ASC
	)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
	) ON [PRIMARY]

	ALTER TABLE [dbo].[BankAccount] WITH CHECK ADD CONSTRAINT [FK_BankAccount_Business] FOREIGN KEY([BusinessId])
	REFERENCES [dbo].[Business] ([Id])
	ALTER TABLE [dbo].[BankAccount] CHECK CONSTRAINT [FK_BankAccount_Business]
END
GO
	
IF NOT EXISTS( SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME='FileConfig' )
BEGIN
	CREATE TABLE [dbo].[FileConfig](
		[Id] [int] IDENTITY(1,1) NOT NULL,
		[EntityName] [varchar](128) NOT NULL,
		[ValidExtension] [varchar](16) NOT NULL,
		[DisplayName] [nvarchar](50) NOT NULL,
		[MaxSizeInbyte] [int] NOT NULL,
		[MinSizeInbyte] [int] NOT NULL,
		[CreatedDate] [datetime] NOT NULL,
		[NamingRule] [nvarchar](64)  NULL
		CONSTRAINT [PK_FileConfig] PRIMARY KEY CLUSTERED 
	(
		[Id] ASC
	)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
	) ON [PRIMARY]
	ALTER TABLE [dbo].[FileConfig] ADD  CONSTRAINT [DF_ileConfig_CreatedDate]  DEFAULT getdate() FOR [CreatedDate]
END
GO

IF NOT EXISTS( SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME='File' )
BEGIN
	CREATE TABLE [dbo].[File](
		[Id] [uniqueidentifier] NOT NULL,
		[Content] [varbinary](max) NULL,
		[FileName] [varchar](64) NOT NULL,
		[Size] [int] NOT NULL,
		[CreatedDate] [datetime] NOT NULL,
		[EntityOwnerId] [uniqueidentifier] NULL,
		[FileConfigId] [int] NOT NULL,
		[BusinessId] [uniqueidentifier] NOT NULL,
		CONSTRAINT [PK_File] PRIMARY KEY CLUSTERED 
	(
		[Id] ASC
	)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
	) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
	ALTER TABLE [dbo].[File] ADD  CONSTRAINT [DF_File_CreatedDate]  DEFAULT (getdate()) FOR [CreatedDate]

	ALTER TABLE [dbo].[File]  WITH CHECK ADD  CONSTRAINT [FK_File_FileConfig] FOREIGN KEY([FileConfigId])
	REFERENCES [dbo].[FileConfig] ([Id])
	ALTER TABLE [dbo].[File] CHECK CONSTRAINT [FK_File_FileConfig]

	ALTER TABLE [dbo].[File] WITH CHECK ADD CONSTRAINT [FK_File_Business] FOREIGN KEY([BusinessId])
	REFERENCES [dbo].[Business] ([Id])
	ALTER TABLE [dbo].[File] CHECK CONSTRAINT [FK_File_Business]
END
GO

IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME='Product')
BEGIN
	CREATE TABLE [dbo].[Product](
		[Id] [uniqueidentifier] NOT NULL,
		[Name] [nvarchar](100) NOT NULL,
		[BuyPrice] [bigint] NOT NULL,
		[SuggestedSellPrice] [bigint] NOT NULL,
		[Stock] [int] NOT NULL,
		[BusinessId] [uniqueidentifier] NOT NULL,
	CONSTRAINT [PK_Product] PRIMARY KEY CLUSTERED 
	(
		[Id] ASC
	) WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
	) ON [PRIMARY]

	ALTER TABLE [dbo].[Product] WITH CHECK ADD CONSTRAINT [FK_Product_Business] FOREIGN KEY([BusinessId])
	REFERENCES [dbo].[Business] ([Id])
	ALTER TABLE [dbo].[Product] CHECK CONSTRAINT [FK_Product_Business]
END
GO
