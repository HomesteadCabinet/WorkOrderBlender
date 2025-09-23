-- DROP SCHEMA dbo;

CREATE SCHEMA dbo;
-- MicrovellumData.dbo.AccountingTemplates definition

-- Drop table

-- DROP TABLE MicrovellumData.dbo.AccountingTemplates;

CREATE TABLE MicrovellumData.dbo.AccountingTemplates (
	CustomColumns nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	DataType int NULL,
	FilePath nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	IncludesHeaders bit NULL,
	LinkID nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDCategory nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	Modified bit NULL,
	MvColumns nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	Name nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[Type] int NULL,
	ID uniqueidentifier DEFAULT newsequentialid() NOT NULL,
	CONSTRAINT PK_AccountingTemplates PRIMARY KEY (ID)
);
 CREATE NONCLUSTERED INDEX idxLinkID ON MicrovellumData.dbo.AccountingTemplates (  LinkID ASC  )  
	 WITH (  PAD_INDEX = OFF ,FILLFACTOR = 100  ,SORT_IN_TEMPDB = OFF , IGNORE_DUP_KEY = OFF , STATISTICS_NORECOMPUTE = OFF , ONLINE = OFF , ALLOW_ROW_LOCKS = ON , ALLOW_PAGE_LOCKS = ON  )
	 ON [PRIMARY ] ;


-- MicrovellumData.dbo.Activities definition

-- Drop table

-- DROP TABLE MicrovellumData.dbo.Activities;

CREATE TABLE MicrovellumData.dbo.Activities (
	ActiveTime float NULL,
	ActualEndDate datetime NULL,
	ActualStartDate datetime NULL,
	Comments nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	Completed bit NULL,
	EndDate datetime NULL,
	GeoLocation nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	GeoLocationEnd nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	IsPublicActivity bit NULL,
	LinkID nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDActivityStation nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDCategory nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDDepartment nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDParent nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDPredecessor nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDProject nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDProjectActivity nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDShift nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDWorkOrder nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	Mobile bit NULL,
	Modified bit NULL,
	Name nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	Overtime bit NULL,
	PredecessorLagDays float NULL,
	PredecessorType int NULL,
	PrintFlag bit NULL,
	StartDate datetime NULL,
	StartDayOfWeek int NULL,
	TardyFlag bit NULL,
	TotalMinutes float NULL,
	[Type] int NULL,
	ID uniqueidentifier DEFAULT newsequentialid() NOT NULL,
	CompletionPercentageOverride float NULL,
	Duration float NULL,
	LagDaysOffset float NULL,
	ScheduledMinutes float NULL,
	SpanProject bit NULL,
	CONSTRAINT PK_Activities PRIMARY KEY (ID)
);
 CREATE NONCLUSTERED INDEX idxLinkID ON MicrovellumData.dbo.Activities (  LinkID ASC  )  
	 WITH (  PAD_INDEX = OFF ,FILLFACTOR = 100  ,SORT_IN_TEMPDB = OFF , IGNORE_DUP_KEY = OFF , STATISTICS_NORECOMPUTE = OFF , ONLINE = OFF , ALLOW_ROW_LOCKS = ON , ALLOW_PAGE_LOCKS = ON  )
	 ON [PRIMARY ] ;


-- MicrovellumData.dbo.ActivityStations definition

-- Drop table

-- DROP TABLE MicrovellumData.dbo.ActivityStations;

CREATE TABLE MicrovellumData.dbo.ActivityStations (
	Comments nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LaborRate float NULL,
	LinkID nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDCategory nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDParentStation nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDProcessingStation nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	Modified bit NULL,
	Name nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	NumberOfEmployeesRequired int NULL,
	StationNumber float NULL,
	UnitsPerMinuteFactoryTime float NULL,
	UnitType int NULL,
	ID uniqueidentifier DEFAULT newsequentialid() NOT NULL,
	CONSTRAINT PK_ActivityStations PRIMARY KEY (ID)
);
 CREATE NONCLUSTERED INDEX idxLinkID ON MicrovellumData.dbo.ActivityStations (  LinkID ASC  )  
	 WITH (  PAD_INDEX = OFF ,FILLFACTOR = 100  ,SORT_IN_TEMPDB = OFF , IGNORE_DUP_KEY = OFF , STATISTICS_NORECOMPUTE = OFF , ONLINE = OFF , ALLOW_ROW_LOCKS = ON , ALLOW_PAGE_LOCKS = ON  )
	 ON [PRIMARY ] ;


-- MicrovellumData.dbo.Attachment definition

-- Drop table

-- DROP TABLE MicrovellumData.dbo.Attachment;

CREATE TABLE MicrovellumData.dbo.Attachment (
	Attachment varbinary(MAX) NULL,
	DateCreated datetime NULL,
	LinkID nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDCategory nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDCreator nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDParent nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	Modified bit NULL,
	Name nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	Notes nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[Size] float NULL,
	ID uniqueidentifier DEFAULT newsequentialid() NOT NULL,
	CONSTRAINT PK_Attachment PRIMARY KEY (ID)
);
 CREATE NONCLUSTERED INDEX idxLinkID ON MicrovellumData.dbo.Attachment (  LinkID ASC  )  
	 WITH (  PAD_INDEX = OFF ,FILLFACTOR = 100  ,SORT_IN_TEMPDB = OFF , IGNORE_DUP_KEY = OFF , STATISTICS_NORECOMPUTE = OFF , ONLINE = OFF , ALLOW_ROW_LOCKS = ON , ALLOW_PAGE_LOCKS = ON  )
	 ON [PRIMARY ] ;


-- MicrovellumData.dbo.AutoCADDrawings definition

-- Drop table

-- DROP TABLE MicrovellumData.dbo.AutoCADDrawings;

CREATE TABLE MicrovellumData.dbo.AutoCADDrawings (
	Drawing varbinary(MAX) NULL,
	JPegName nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	JPegStream varbinary(MAX) NULL,
	LinkID nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDCategory nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	Modified bit NULL,
	Name nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	TiffName nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	TiffStream varbinary(MAX) NULL,
	[Type] int NULL,
	WMFName nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	WMFStream varbinary(MAX) NULL,
	ID uniqueidentifier DEFAULT newsequentialid() NOT NULL,
	CONSTRAINT PK_AutoCADDrawings PRIMARY KEY (ID)
);
 CREATE NONCLUSTERED INDEX idxLinkID ON MicrovellumData.dbo.AutoCADDrawings (  LinkID ASC  )  
	 WITH (  PAD_INDEX = OFF ,FILLFACTOR = 100  ,SORT_IN_TEMPDB = OFF , IGNORE_DUP_KEY = OFF , STATISTICS_NORECOMPUTE = OFF , ONLINE = OFF , ALLOW_ROW_LOCKS = ON , ALLOW_PAGE_LOCKS = ON  )
	 ON [PRIMARY ] ;


-- MicrovellumData.dbo.BiesseMachineSetting definition

-- Drop table

-- DROP TABLE MicrovellumData.dbo.BiesseMachineSetting;

CREATE TABLE MicrovellumData.dbo.BiesseMachineSetting (
	LinkID nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	MaximumLength float NULL,
	MaximumThickness float NULL,
	MaximumWidth float NULL,
	MinimumLength float NULL,
	MinimumThickness float NULL,
	MinimumWidth float NULL,
	ID uniqueidentifier DEFAULT newsequentialid() NOT NULL,
	CONSTRAINT PK_BiesseMachineSetting PRIMARY KEY (ID)
);
 CREATE NONCLUSTERED INDEX idxLinkID ON MicrovellumData.dbo.BiesseMachineSetting (  LinkID ASC  )  
	 WITH (  PAD_INDEX = OFF ,FILLFACTOR = 100  ,SORT_IN_TEMPDB = OFF , IGNORE_DUP_KEY = OFF , STATISTICS_NORECOMPUTE = OFF , ONLINE = OFF , ALLOW_ROW_LOCKS = ON , ALLOW_PAGE_LOCKS = ON  )
	 ON [PRIMARY ] ;


-- MicrovellumData.dbo.BluePrintViews definition

-- Drop table

-- DROP TABLE MicrovellumData.dbo.BluePrintViews;

CREATE TABLE MicrovellumData.dbo.BluePrintViews (
	BlueprintViewType int NULL,
	LinkID nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDCategory nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDLocation nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDProject nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDTiffDrawing1 nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDTiffDrawing2 nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDTiffDrawing3 nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDTiffDrawing4 nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDTiffDrawing5 nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	Modified bit NULL,
	Name nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	ViewerHeight1 int NULL,
	ViewerHeight2 int NULL,
	ViewerHeight3 int NULL,
	ViewerHeight4 int NULL,
	ViewerHeight5 int NULL,
	ViewerLeft1 int NULL,
	ViewerLeft2 int NULL,
	ViewerLeft3 int NULL,
	ViewerLeft4 int NULL,
	ViewerLeft5 int NULL,
	ViewerTop1 int NULL,
	ViewerTop2 int NULL,
	ViewerTop3 int NULL,
	ViewerTop4 int NULL,
	ViewerTop5 int NULL,
	ViewerWidth1 int NULL,
	ViewerWidth2 int NULL,
	ViewerWidth3 int NULL,
	ViewerWidth4 int NULL,
	ViewerWidth5 int NULL,
	ID uniqueidentifier DEFAULT newsequentialid() NOT NULL,
	CONSTRAINT PK_BluePrintViews PRIMARY KEY (ID)
);
 CREATE NONCLUSTERED INDEX idxLinkID ON MicrovellumData.dbo.BluePrintViews (  LinkID ASC  )  
	 WITH (  PAD_INDEX = OFF ,FILLFACTOR = 100  ,SORT_IN_TEMPDB = OFF , IGNORE_DUP_KEY = OFF , STATISTICS_NORECOMPUTE = OFF , ONLINE = OFF , ALLOW_ROW_LOCKS = ON , ALLOW_PAGE_LOCKS = ON  )
	 ON [PRIMARY ] ;


-- MicrovellumData.dbo.Breaks definition

-- Drop table

-- DROP TABLE MicrovellumData.dbo.Breaks;

CREATE TABLE MicrovellumData.dbo.Breaks (
	EndTime datetime NULL,
	LinkID nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDCategory nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	Modified bit NULL,
	Name nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	PaidBreak bit NULL,
	StartTime datetime NULL,
	ID uniqueidentifier DEFAULT newsequentialid() NOT NULL,
	CONSTRAINT PK_Breaks PRIMARY KEY (ID)
);
 CREATE NONCLUSTERED INDEX idxLinkID ON MicrovellumData.dbo.Breaks (  LinkID ASC  )  
	 WITH (  PAD_INDEX = OFF ,FILLFACTOR = 100  ,SORT_IN_TEMPDB = OFF , IGNORE_DUP_KEY = OFF , STATISTICS_NORECOMPUTE = OFF , ONLINE = OFF , ALLOW_ROW_LOCKS = ON , ALLOW_PAGE_LOCKS = ON  )
	 ON [PRIMARY ] ;


-- MicrovellumData.dbo.BundleItemSettings definition

-- Drop table

-- DROP TABLE MicrovellumData.dbo.BundleItemSettings;

CREATE TABLE MicrovellumData.dbo.BundleItemSettings (
	BundleByComment bit NULL,
	BundleByProduct bit NULL,
	BundleByRoom bit NULL,
	BundleBySize bit NULL,
	BundleLikeItems bit NULL,
	LinkID nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	MaxPieceCount int NULL,
	[Type] int NULL,
	ID uniqueidentifier DEFAULT newsequentialid() NOT NULL,
	CONSTRAINT PK_BundleItemSettings PRIMARY KEY (ID)
);
 CREATE NONCLUSTERED INDEX idxLinkID ON MicrovellumData.dbo.BundleItemSettings (  LinkID ASC  )  
	 WITH (  PAD_INDEX = OFF ,FILLFACTOR = 100  ,SORT_IN_TEMPDB = OFF , IGNORE_DUP_KEY = OFF , STATISTICS_NORECOMPUTE = OFF , ONLINE = OFF , ALLOW_ROW_LOCKS = ON , ALLOW_PAGE_LOCKS = ON  )
	 ON [PRIMARY ] ;


-- MicrovellumData.dbo.BundleItems definition

-- Drop table

-- DROP TABLE MicrovellumData.dbo.BundleItems;

CREATE TABLE MicrovellumData.dbo.BundleItems (
	Comments nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[Depth] float NULL,
	[Length] float NULL,
	LinkID nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDBundle nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDItem nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDProduct nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDProject nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDRoom nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDWorkOrder nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	Name nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	Quantity float NULL,
	ScanCode nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[Type] int NULL,
	Width float NULL,
	ID uniqueidentifier DEFAULT newsequentialid() NOT NULL,
	CONSTRAINT PK_BundleItems PRIMARY KEY (ID)
);
 CREATE NONCLUSTERED INDEX idxLinkID ON MicrovellumData.dbo.BundleItems (  LinkID ASC  )  
	 WITH (  PAD_INDEX = OFF ,FILLFACTOR = 100  ,SORT_IN_TEMPDB = OFF , IGNORE_DUP_KEY = OFF , STATISTICS_NORECOMPUTE = OFF , ONLINE = OFF , ALLOW_ROW_LOCKS = ON , ALLOW_PAGE_LOCKS = ON  )
	 ON [PRIMARY ] ;


-- MicrovellumData.dbo.Bundles definition

-- Drop table

-- DROP TABLE MicrovellumData.dbo.Bundles;

CREATE TABLE MicrovellumData.dbo.Bundles (
	Comments nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	DateCreated datetime NULL,
	LinkID nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDEmployee nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDProject nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDStorageGroup nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDStorageLocation nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	Name nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	Number nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	PrintFlag bit NULL,
	ScanCode nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	Status int NULL,
	[Type] int NULL,
	ID uniqueidentifier DEFAULT newsequentialid() NOT NULL,
	CONSTRAINT PK_Bundles PRIMARY KEY (ID)
);
 CREATE NONCLUSTERED INDEX idxLinkID ON MicrovellumData.dbo.Bundles (  LinkID ASC  )  
	 WITH (  PAD_INDEX = OFF ,FILLFACTOR = 100  ,SORT_IN_TEMPDB = OFF , IGNORE_DUP_KEY = OFF , STATISTICS_NORECOMPUTE = OFF , ONLINE = OFF , ALLOW_ROW_LOCKS = ON , ALLOW_PAGE_LOCKS = ON  )
	 ON [PRIMARY ] ;


-- MicrovellumData.dbo.Categories definition

-- Drop table

-- DROP TABLE MicrovellumData.dbo.Categories;

CREATE TABLE MicrovellumData.dbo.Categories (
	CategoryFilter int NULL,
	CategoryLevel int NULL,
	IsDefault bit NULL,
	LinkID nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDCategory nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDLibrary nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDParent nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	Modified bit NULL,
	Name nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[Type] int NULL,
	Visible bit NULL,
	ID uniqueidentifier DEFAULT newsequentialid() NOT NULL,
	IsExpanded bit NULL,
	CONSTRAINT PK_Categories PRIMARY KEY (ID)
);
 CREATE NONCLUSTERED INDEX idxLinkID ON MicrovellumData.dbo.Categories (  LinkID ASC  )  
	 WITH (  PAD_INDEX = OFF ,FILLFACTOR = 100  ,SORT_IN_TEMPDB = OFF , IGNORE_DUP_KEY = OFF , STATISTICS_NORECOMPUTE = OFF , ONLINE = OFF , ALLOW_ROW_LOCKS = ON , ALLOW_PAGE_LOCKS = ON  )
	 ON [PRIMARY ] ;


-- MicrovellumData.dbo.CommonCommandsGrids definition

-- Drop table

-- DROP TABLE MicrovellumData.dbo.CommonCommandsGrids;

CREATE TABLE MicrovellumData.dbo.CommonCommandsGrids (
	LinkID nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDCategory nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	Modified bit NULL,
	Name nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	WorkBook varbinary(MAX) NULL,
	ID uniqueidentifier DEFAULT newsequentialid() NOT NULL,
	CONSTRAINT PK_CommonCommandsGrids PRIMARY KEY (ID)
);
 CREATE NONCLUSTERED INDEX idxLinkID ON MicrovellumData.dbo.CommonCommandsGrids (  LinkID ASC  )  
	 WITH (  PAD_INDEX = OFF ,FILLFACTOR = 100  ,SORT_IN_TEMPDB = OFF , IGNORE_DUP_KEY = OFF , STATISTICS_NORECOMPUTE = OFF , ONLINE = OFF , ALLOW_ROW_LOCKS = ON , ALLOW_PAGE_LOCKS = ON  )
	 ON [PRIMARY ] ;


-- MicrovellumData.dbo.Configurations definition

-- Drop table

-- DROP TABLE MicrovellumData.dbo.Configurations;

CREATE TABLE MicrovellumData.dbo.Configurations (
	AllowNotifications bit NULL,
	AllowWorkOrderCreation bit NULL,
	AllowWorkOrderProcessing bit NULL,
	ConfigurationType int NULL,
	DataPath nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkID nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	Name nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	NotificationPassword nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	Port int NULL,
	ServerAllowsNotifications bit NULL,
	ServerAllowsWorkOrderCreation bit NULL,
	ServerAllowsWorkOrderProcessing bit NULL,
	ServerName nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	UserPrincipalName nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	ID uniqueidentifier DEFAULT newsequentialid() NOT NULL,
	CONSTRAINT PK_Configurations PRIMARY KEY (ID)
);
 CREATE NONCLUSTERED INDEX idxLinkID ON MicrovellumData.dbo.Configurations (  LinkID ASC  )  
	 WITH (  PAD_INDEX = OFF ,FILLFACTOR = 100  ,SORT_IN_TEMPDB = OFF , IGNORE_DUP_KEY = OFF , STATISTICS_NORECOMPUTE = OFF , ONLINE = OFF , ALLOW_ROW_LOCKS = ON , ALLOW_PAGE_LOCKS = ON  )
	 ON [PRIMARY ] ;


-- MicrovellumData.dbo.Contacts definition

-- Drop table

-- DROP TABLE MicrovellumData.dbo.Contacts;

CREATE TABLE MicrovellumData.dbo.Contacts (
	Age int NULL,
	Anniversary datetime NULL,
	Birthday datetime NULL,
	CellPhone nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	City nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	Comments nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	Country nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	DateFirstContact datetime NULL,
	Department nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	EmailDefault nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	FaxDefault nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	FirstName nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	HomePhone nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	JPegName nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	JPegStream varbinary(MAX) NULL,
	LastName nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkID nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDCategory nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDCompany nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	MailCity nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	MailCountry nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	MailPOBox nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	MailState nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	MailStreet nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	MailZipCode nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	ManagerName nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	MiddleName nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	Modified bit NULL,
	NameNickname nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	NamePartner nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	NameSuffix nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	Office nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	OtherAddressStreet nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	PhoneDefault nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	POBox nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	Profession nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	RestrictAccess nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	Salutation nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	State nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	Street nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	TiffName nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	TiffStream varbinary(MAX) NULL,
	Title nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	WebSiteDefault nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	WMFName nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	WMFStream varbinary(MAX) NULL,
	ZipCode nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	ID uniqueidentifier DEFAULT newsequentialid() NOT NULL,
	[Type] int NULL,
	CONSTRAINT PK_Contacts PRIMARY KEY (ID)
);
 CREATE NONCLUSTERED INDEX idxLinkID ON MicrovellumData.dbo.Contacts (  LinkID ASC  )  
	 WITH (  PAD_INDEX = OFF ,FILLFACTOR = 100  ,SORT_IN_TEMPDB = OFF , IGNORE_DUP_KEY = OFF , STATISTICS_NORECOMPUTE = OFF , ONLINE = OFF , ALLOW_ROW_LOCKS = ON , ALLOW_PAGE_LOCKS = ON  )
	 ON [PRIMARY ] ;


-- MicrovellumData.dbo.Correspondence definition

-- Drop table

-- DROP TABLE MicrovellumData.dbo.Correspondence;

CREATE TABLE MicrovellumData.dbo.Correspondence (
	Body nvarchar(MAX) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	DateCreated datetime NULL,
	EmailAddress nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkID nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDCategory nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDParent nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	Modified bit NULL,
	Name nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	ReceivedByName nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	SenderName nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	Subject nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	TheDate datetime NULL,
	[Type] int NULL,
	ID uniqueidentifier DEFAULT newsequentialid() NOT NULL,
	CONSTRAINT PK_Correspondence PRIMARY KEY (ID)
);
 CREATE NONCLUSTERED INDEX idxLinkID ON MicrovellumData.dbo.Correspondence (  LinkID ASC  )  
	 WITH (  PAD_INDEX = OFF ,FILLFACTOR = 100  ,SORT_IN_TEMPDB = OFF , IGNORE_DUP_KEY = OFF , STATISTICS_NORECOMPUTE = OFF , ONLINE = OFF , ALLOW_ROW_LOCKS = ON , ALLOW_PAGE_LOCKS = ON  )
	 ON [PRIMARY ] ;


-- MicrovellumData.dbo.CutPartsFiles definition

-- Drop table

-- DROP TABLE MicrovellumData.dbo.CutPartsFiles;

CREATE TABLE MicrovellumData.dbo.CutPartsFiles (
	LinkID nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDCategory nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDLibrary nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDProject nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	Modified bit NULL,
	Name nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[Type] int NULL,
	WorkBook varbinary(MAX) NULL,
	ID uniqueidentifier DEFAULT newsequentialid() NOT NULL,
	CONSTRAINT PK_CutPartsFiles PRIMARY KEY (ID)
);
 CREATE NONCLUSTERED INDEX idxLinkID ON MicrovellumData.dbo.CutPartsFiles (  LinkID ASC  )  
	 WITH (  PAD_INDEX = OFF ,FILLFACTOR = 100  ,SORT_IN_TEMPDB = OFF , IGNORE_DUP_KEY = OFF , STATISTICS_NORECOMPUTE = OFF , ONLINE = OFF , ALLOW_ROW_LOCKS = ON , ALLOW_PAGE_LOCKS = ON  )
	 ON [PRIMARY ] ;


-- MicrovellumData.dbo.Departments definition

-- Drop table

-- DROP TABLE MicrovellumData.dbo.Departments;

CREATE TABLE MicrovellumData.dbo.Departments (
	DepartmentManager nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	DepartmentNumber float NULL,
	LinkID nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDCategory nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	Modified bit NULL,
	Name nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	ResourceHours float NULL,
	TargetNumberOfEmployees float NULL,
	ID uniqueidentifier DEFAULT newsequentialid() NOT NULL,
	LinkIDPrimaryShift nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	CONSTRAINT PK_Departments PRIMARY KEY (ID)
);
 CREATE NONCLUSTERED INDEX idxLinkID ON MicrovellumData.dbo.Departments (  LinkID ASC  )  
	 WITH (  PAD_INDEX = OFF ,FILLFACTOR = 100  ,SORT_IN_TEMPDB = OFF , IGNORE_DUP_KEY = OFF , STATISTICS_NORECOMPUTE = OFF , ONLINE = OFF , ALLOW_ROW_LOCKS = ON , ALLOW_PAGE_LOCKS = ON  )
	 ON [PRIMARY ] ;


-- MicrovellumData.dbo.DoorLayers definition

-- Drop table

-- DROP TABLE MicrovellumData.dbo.DoorLayers;

CREATE TABLE MicrovellumData.dbo.DoorLayers (
	LinkID nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDCategory nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDLibrary nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	Modified bit NULL,
	Name nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	ID uniqueidentifier DEFAULT newsequentialid() NOT NULL,
	CONSTRAINT PK_DoorLayers PRIMARY KEY (ID)
);
 CREATE NONCLUSTERED INDEX idxLinkID ON MicrovellumData.dbo.DoorLayers (  LinkID ASC  )  
	 WITH (  PAD_INDEX = OFF ,FILLFACTOR = 100  ,SORT_IN_TEMPDB = OFF , IGNORE_DUP_KEY = OFF , STATISTICS_NORECOMPUTE = OFF , ONLINE = OFF , ALLOW_ROW_LOCKS = ON , ALLOW_PAGE_LOCKS = ON  )
	 ON [PRIMARY ] ;


-- MicrovellumData.dbo.DoorWizardFiles definition

-- Drop table

-- DROP TABLE MicrovellumData.dbo.DoorWizardFiles;

CREATE TABLE MicrovellumData.dbo.DoorWizardFiles (
	LinkID nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDCategory nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDLibrary nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDProject nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	Modified bit NULL,
	Name nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[Type] int NULL,
	WorkBook varbinary(MAX) NULL,
	ID uniqueidentifier DEFAULT newsequentialid() NOT NULL,
	CONSTRAINT PK_DoorWizardFiles PRIMARY KEY (ID)
);
 CREATE NONCLUSTERED INDEX idxLinkID ON MicrovellumData.dbo.DoorWizardFiles (  LinkID ASC  )  
	 WITH (  PAD_INDEX = OFF ,FILLFACTOR = 100  ,SORT_IN_TEMPDB = OFF , IGNORE_DUP_KEY = OFF , STATISTICS_NORECOMPUTE = OFF , ONLINE = OFF , ALLOW_ROW_LOCKS = ON , ALLOW_PAGE_LOCKS = ON  )
	 ON [PRIMARY ] ;


-- MicrovellumData.dbo.EdgebandFiles definition

-- Drop table

-- DROP TABLE MicrovellumData.dbo.EdgebandFiles;

CREATE TABLE MicrovellumData.dbo.EdgebandFiles (
	LinkID nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDCategory nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDLibrary nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDProject nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	Modified bit NULL,
	Name nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[Type] int NULL,
	WorkBook varbinary(MAX) NULL,
	ID uniqueidentifier DEFAULT newsequentialid() NOT NULL,
	CONSTRAINT PK_EdgebandFiles PRIMARY KEY (ID)
);
 CREATE NONCLUSTERED INDEX idxLinkID ON MicrovellumData.dbo.EdgebandFiles (  LinkID ASC  )  
	 WITH (  PAD_INDEX = OFF ,FILLFACTOR = 100  ,SORT_IN_TEMPDB = OFF , IGNORE_DUP_KEY = OFF , STATISTICS_NORECOMPUTE = OFF , ONLINE = OFF , ALLOW_ROW_LOCKS = ON , ALLOW_PAGE_LOCKS = ON  )
	 ON [PRIMARY ] ;


-- MicrovellumData.dbo.Edgebanding definition

-- Drop table

-- DROP TABLE MicrovellumData.dbo.Edgebanding;

CREATE TABLE MicrovellumData.dbo.Edgebanding (
	Code nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	Comments nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	CustomSizeAdjustment float NULL,
	Face int NULL,
	Grain int NULL,
	HatchType int NULL,
	InventoryAvailableQty float NULL,
	InventoryCurrentQty float NULL,
	InventoryMinQty float NULL,
	LinFt float NULL,
	LinkID nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDBottomFaceRendering nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDCategory nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDCoreRendering nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDDefaultVendor nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDMaterial nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDPart nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDProduct nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDProject nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDSheetSize nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDSubAssembly nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDTopFaceRendering nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDWorkOrder nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	MarkUp float NULL,
	Modified bit NULL,
	Name nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	Quantity float NULL,
	SqFt float NULL,
	Thickness float NULL,
	[Type] int NULL,
	UnitType int NULL,
	VendorCost float NULL,
	WasteFactor float NULL,
	ID uniqueidentifier DEFAULT newsequentialid() NOT NULL,
	HandlingCode nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	CodeFormula varchar(MAX) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	GrainFormula varchar(MAX) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	HandlingCodeFormula varchar(MAX) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	IsFormulaMaterial bit NULL,
	MarkUpFormula varchar(MAX) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	MaterialCommentsFormula varchar(MAX) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	MaterialFlipSetting int NULL,
	WasteFactorFormula varchar(MAX) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	CONSTRAINT PK_Edgebanding PRIMARY KEY (ID)
);
 CREATE NONCLUSTERED INDEX idxLinkID ON MicrovellumData.dbo.Edgebanding (  LinkID ASC  )  
	 WITH (  PAD_INDEX = OFF ,FILLFACTOR = 100  ,SORT_IN_TEMPDB = OFF , IGNORE_DUP_KEY = OFF , STATISTICS_NORECOMPUTE = OFF , ONLINE = OFF , ALLOW_ROW_LOCKS = ON , ALLOW_PAGE_LOCKS = ON  )
	 ON [PRIMARY ] ;


-- MicrovellumData.dbo.EmployeeLogin definition

-- Drop table

-- DROP TABLE MicrovellumData.dbo.EmployeeLogin;

CREATE TABLE MicrovellumData.dbo.EmployeeLogin (
	AllowLogin bit NULL,
	LinkID nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDCategory nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	Modified bit NULL,
	Name nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	ID uniqueidentifier DEFAULT newsequentialid() NOT NULL,
	AllowSecurityByPass bit NULL,
	CONSTRAINT PK_EmployeeLogin PRIMARY KEY (ID)
);
 CREATE NONCLUSTERED INDEX idxLinkID ON MicrovellumData.dbo.EmployeeLogin (  LinkID ASC  )  
	 WITH (  PAD_INDEX = OFF ,FILLFACTOR = 100  ,SORT_IN_TEMPDB = OFF , IGNORE_DUP_KEY = OFF , STATISTICS_NORECOMPUTE = OFF , ONLINE = OFF , ALLOW_ROW_LOCKS = ON , ALLOW_PAGE_LOCKS = ON  )
	 ON [PRIMARY ] ;


-- MicrovellumData.dbo.EmployeeSecure definition

-- Drop table

-- DROP TABLE MicrovellumData.dbo.EmployeeSecure;

CREATE TABLE MicrovellumData.dbo.EmployeeSecure (
	AllowAccessLibraryDesigner bit NULL,
	AllowAccesstoDatabaseExplorer bit NULL,
	AllowAccesstoDatabaseUtilities bit NULL,
	AllowAccesstoTransferTables bit NULL,
	AllowCompanyManagement bit NULL,
	AllowEditTimeClock bit NULL,
	AllowEstimating bit NULL,
	AllowImportData bit NULL,
	AllowProduction bit NULL,
	AllowPurchasing bit NULL,
	AllowSaveProductstoLibrary bit NULL,
	AllowSaveSpecificationGroupstoTemplate bit NULL,
	AllowSaveSubassembliestoLibrary bit NULL,
	AllowScheduling bit NULL,
	AllowSetup bit NULL,
	AllowUpdates bit NULL,
	EmployeePassword nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	HourlyPay float NULL,
	LinkID nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDCategory nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDEmployee nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	MmAlertSettings int NULL,
	Modified bit NULL,
	Name nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	SoftwareSecurityType int NULL,
	SSNumber nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	WebSupportRolls int NULL,
	ID uniqueidentifier DEFAULT newsequentialid() NOT NULL,
	AllowEventLogArchive bit NULL,
	AllowEventLogDelete bit NULL,
	AllowAddProjectComponentFromTemplate bit NULL,
	AllowCopyProjectComponent bit NULL,
	AllowCopyProjectSpecGroup bit NULL,
	AllowCopyTemplateComponent bit NULL,
	AllowCopyTemplateSpecGroup bit NULL,
	AllowDeleteProjectComponent bit NULL,
	AllowDeleteProjectSpecificationGroup bit NULL,
	AllowDeleteTemplateComponent bit NULL,
	AllowDeleteTemplateSpecificationGroup bit NULL,
	AllowImportProjectSpecGroup bit NULL,
	AllowOptionsData bit NULL,
	AllowOptionsGeneral bit NULL,
	AllowOptionsMachining bit NULL,
	AllowProcessingStationManagement bit NULL,
	AllowProjectCutPartWizard bit NULL,
	AllowProjectDoorWizard bit NULL,
	AllowProjectEdgeBandWizard bit NULL,
	AllowProjectGlobals bit NULL,
	AllowProjectHardwareWizard bit NULL,
	AllowProjectMaterials bit NULL,
	AllowProjectWizard bit NULL,
	AllowProjectWorkBookDesigner bit NULL,
	AllowRenameProjectomponent bit NULL,
	AllowRenameProjectSpecGroup bit NULL,
	AllowRenameTemplateComponent bit NULL,
	AllowRenameTemplateSpecGroup bit NULL,
	AllowReportDesigner bit NULL,
	AllowRevertProjectComponent bit NULL,
	AllowSaveProjectComponentToTemplate bit NULL,
	AllowSaveProjectSpecificationGroupToTemplate bit NULL,
	AllowTemplateCutPartWizard bit NULL,
	AllowTemplateDoorWizard bit NULL,
	AllowTemplateEdgeBandWizard bit NULL,
	AllowTemplateGlobals bit NULL,
	AllowTemplateHardwareWizard bit NULL,
	AllowTemplateMaterials bit NULL,
	AllowTemplateProjectWizard bit NULL,
	AllowTemplateWorkBookDesigner bit NULL,
	AllowToolFileManagement bit NULL,
	AllowAssignComponentToProjectSpecGroup bit NULL,
	AllowAssignComponentToTemplateSpecGroup bit NULL,
	AllowProjectDelete int NULL,
	AllowDBUExport bit NULL,
	AllowDBUExportAndDelete bit NULL,
	AllowDBUImport bit NULL,
	AllowDBUImportAndOverwrite bit NULL,
	AllowWorkOrderModify bit NULL,
	AllowProjectRename bit NULL,
	AccessEventLogProperties bit NULL,
	AccessLibraryUpdateUtility bit NULL,
	AllowProjectFactoryWorkbook bit NULL,
	CONSTRAINT PK_EmployeeSecure PRIMARY KEY (ID)
);
 CREATE NONCLUSTERED INDEX idxLinkID ON MicrovellumData.dbo.EmployeeSecure (  LinkID ASC  )  
	 WITH (  PAD_INDEX = OFF ,FILLFACTOR = 100  ,SORT_IN_TEMPDB = OFF , IGNORE_DUP_KEY = OFF , STATISTICS_NORECOMPUTE = OFF , ONLINE = OFF , ALLOW_ROW_LOCKS = ON , ALLOW_PAGE_LOCKS = ON  )
	 ON [PRIMARY ] ;


-- MicrovellumData.dbo.Employees definition

-- Drop table

-- DROP TABLE MicrovellumData.dbo.Employees;

CREATE TABLE MicrovellumData.dbo.Employees (
	Age int NULL,
	Anniversary datetime NULL,
	Birthday datetime NULL,
	CellPhone nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	City nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	Comments nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	Country nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	DateFirstContact datetime NULL,
	Department nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	Department1HoursAllocation float NULL,
	Department2HoursAllocation float NULL,
	Department3HoursAllocation float NULL,
	EmailDefault nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	FaxDefault nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	FirstName nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	HomePhone nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	JPegName nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	JPegStream varbinary(MAX) NULL,
	LastName nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkID nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDActivityStation nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDCategory nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDCompany nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDPrimaryDepartment nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDSecondaryDepartment nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDShift nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDTertiaryDepartment nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	MailCity nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	MailCountry nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	MailPOBox nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	MailState nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	MailStreet nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	MailZipCode nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	ManagerName nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	MiddleName nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	Modified bit NULL,
	NameNickname nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	NamePartner nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	NameSuffix nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	Office nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	OtherAddressStreet nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	PhoneDefault nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	POBox nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	Profession nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	RestrictAccess nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	Salutation nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	State nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	Street nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	TiffName nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	TiffStream varbinary(MAX) NULL,
	Title nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	WebSiteDefault nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	WMFName nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	WMFStream varbinary(MAX) NULL,
	ZipCode nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	ID uniqueidentifier DEFAULT newsequentialid() NOT NULL,
	Department1Efficiency float NULL,
	Department2Efficiency float NULL,
	Department3Efficiency float NULL,
	LinkIDSecondaryShift nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDTertiaryShift nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[Type] int NULL,
	CONSTRAINT PK_Employees PRIMARY KEY (ID)
);
 CREATE NONCLUSTERED INDEX idxLinkID ON MicrovellumData.dbo.Employees (  LinkID ASC  )  
	 WITH (  PAD_INDEX = OFF ,FILLFACTOR = 100  ,SORT_IN_TEMPDB = OFF , IGNORE_DUP_KEY = OFF , STATISTICS_NORECOMPUTE = OFF , ONLINE = OFF , ALLOW_ROW_LOCKS = ON , ALLOW_PAGE_LOCKS = ON  )
	 ON [PRIMARY ] ;


-- MicrovellumData.dbo.ErrorMessages definition

-- Drop table

-- DROP TABLE MicrovellumData.dbo.ErrorMessages;

CREATE TABLE MicrovellumData.dbo.ErrorMessages (
	LinkID nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDCategory nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDGCodeResult nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDProcessingStation nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDWorkOrder nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDWorkOrderBatch nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	Modified bit NULL,
	Name nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	Title nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[Type] int NULL,
	ID uniqueidentifier DEFAULT newsequentialid() NOT NULL,
	CONSTRAINT PK_ErrorMessages PRIMARY KEY (ID)
);
 CREATE NONCLUSTERED INDEX idxLinkID ON MicrovellumData.dbo.ErrorMessages (  LinkID ASC  )  
	 WITH (  PAD_INDEX = OFF ,FILLFACTOR = 100  ,SORT_IN_TEMPDB = OFF , IGNORE_DUP_KEY = OFF , STATISTICS_NORECOMPUTE = OFF , ONLINE = OFF , ALLOW_ROW_LOCKS = ON , ALLOW_PAGE_LOCKS = ON  )
	 ON [PRIMARY ] ;


-- MicrovellumData.dbo.Estimates definition

-- Drop table

-- DROP TABLE MicrovellumData.dbo.Estimates;

CREATE TABLE MicrovellumData.dbo.Estimates (
	LinkID nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDCategory nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	Modified bit NULL,
	Name nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	ID uniqueidentifier DEFAULT newsequentialid() NOT NULL,
	CONSTRAINT PK_Estimates PRIMARY KEY (ID)
);
 CREATE NONCLUSTERED INDEX idxLinkID ON MicrovellumData.dbo.Estimates (  LinkID ASC  )  
	 WITH (  PAD_INDEX = OFF ,FILLFACTOR = 100  ,SORT_IN_TEMPDB = OFF , IGNORE_DUP_KEY = OFF , STATISTICS_NORECOMPUTE = OFF , ONLINE = OFF , ALLOW_ROW_LOCKS = ON , ALLOW_PAGE_LOCKS = ON  )
	 ON [PRIMARY ] ;


-- MicrovellumData.dbo.EventLogs definition

-- Drop table

-- DROP TABLE MicrovellumData.dbo.EventLogs;

CREATE TABLE MicrovellumData.dbo.EventLogs (
	AlertType int NULL,
	Archived bit NULL,
	[DateTime] datetime NULL,
	Description nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	EventType int NULL,
	InnerMessage nvarchar(MAX) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkID nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDGrandParent nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDParent nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	MachineName nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	Message nvarchar(MAX) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	SeverityLevel int NULL,
	[Source] int NULL,
	StackTrace nvarchar(MAX) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[User] nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	ID uniqueidentifier DEFAULT newsequentialid() NOT NULL,
	CONSTRAINT PK_EventLogs PRIMARY KEY (ID)
);
 CREATE NONCLUSTERED INDEX idxLinkID ON MicrovellumData.dbo.EventLogs (  LinkID ASC  )  
	 WITH (  PAD_INDEX = OFF ,FILLFACTOR = 100  ,SORT_IN_TEMPDB = OFF , IGNORE_DUP_KEY = OFF , STATISTICS_NORECOMPUTE = OFF , ONLINE = OFF , ALLOW_ROW_LOCKS = ON , ALLOW_PAGE_LOCKS = ON  )
	 ON [PRIMARY ] ;
 CREATE NONCLUSTERED INDEX lookup_idx ON MicrovellumData.dbo.EventLogs (  Archived ASC  )  
	 INCLUDE ( LinkID ) 
	 WITH (  PAD_INDEX = OFF ,FILLFACTOR = 100  ,SORT_IN_TEMPDB = OFF , IGNORE_DUP_KEY = OFF , STATISTICS_NORECOMPUTE = OFF , ONLINE = OFF , ALLOW_ROW_LOCKS = ON , ALLOW_PAGE_LOCKS = ON  )
	 ON [PRIMARY ] ;


-- MicrovellumData.dbo.FaceFrameImages definition

-- Drop table

-- DROP TABLE MicrovellumData.dbo.FaceFrameImages;

CREATE TABLE MicrovellumData.dbo.FaceFrameImages (
	JPegName nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	JPegStream varbinary(MAX) NULL,
	LinkID nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDCategory nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDProduct nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDWorkOrder nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	Modified bit NULL,
	Name nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	TiffName nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	TiffStream varbinary(MAX) NULL,
	[Type] int NULL,
	WMFName nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	WMFStream varbinary(MAX) NULL,
	ID uniqueidentifier DEFAULT newsequentialid() NOT NULL,
	CONSTRAINT PK_FaceFrameImages PRIMARY KEY (ID)
);
 CREATE NONCLUSTERED INDEX idxLinkID ON MicrovellumData.dbo.FaceFrameImages (  LinkID ASC  )  
	 WITH (  PAD_INDEX = OFF ,FILLFACTOR = 100  ,SORT_IN_TEMPDB = OFF , IGNORE_DUP_KEY = OFF , STATISTICS_NORECOMPUTE = OFF , ONLINE = OFF , ALLOW_ROW_LOCKS = ON , ALLOW_PAGE_LOCKS = ON  )
	 ON [PRIMARY ] ;


-- MicrovellumData.dbo.FaceFrameImagesParts definition

-- Drop table

-- DROP TABLE MicrovellumData.dbo.FaceFrameImagesParts;

CREATE TABLE MicrovellumData.dbo.FaceFrameImagesParts (
	LinkIDCategory nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDFaceFrameImage nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDPart nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDWorkOrder nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	Modified bit NULL,
	ID uniqueidentifier DEFAULT newsequentialid() NOT NULL,
	CONSTRAINT PK_FaceFrameImagesParts PRIMARY KEY (ID)
);


-- MicrovellumData.dbo.FaceFrameImagesSubassemblies definition

-- Drop table

-- DROP TABLE MicrovellumData.dbo.FaceFrameImagesSubassemblies;

CREATE TABLE MicrovellumData.dbo.FaceFrameImagesSubassemblies (
	LinkIDCategory nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDFaceFrameImage nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDSubassembly nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDWorkOrder nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	Modified bit NULL,
	ID uniqueidentifier DEFAULT newsequentialid() NOT NULL,
	CONSTRAINT PK_FaceFrameImagesSubassemblies PRIMARY KEY (ID)
);


-- MicrovellumData.dbo.Factory definition

-- Drop table

-- DROP TABLE MicrovellumData.dbo.Factory;

CREATE TABLE MicrovellumData.dbo.Factory (
	LinkID nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDCategory nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDLibrary nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDProject nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	Modified bit NULL,
	Name nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[Type] int NULL,
	WorkBook varbinary(MAX) NULL,
	ID uniqueidentifier DEFAULT newsequentialid() NOT NULL,
	CONSTRAINT PK_Factory PRIMARY KEY (ID)
);
 CREATE NONCLUSTERED INDEX idxLinkID ON MicrovellumData.dbo.Factory (  LinkID ASC  )  
	 WITH (  PAD_INDEX = OFF ,FILLFACTOR = 100  ,SORT_IN_TEMPDB = OFF , IGNORE_DUP_KEY = OFF , STATISTICS_NORECOMPUTE = OFF , ONLINE = OFF , ALLOW_ROW_LOCKS = ON , ALLOW_PAGE_LOCKS = ON  )
	 ON [PRIMARY ] ;


-- MicrovellumData.dbo.GeneralContacts definition

-- Drop table

-- DROP TABLE MicrovellumData.dbo.GeneralContacts;

CREATE TABLE MicrovellumData.dbo.GeneralContacts (
	CellPhone nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	City nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	Comments nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	Country nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	EmailDefault nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	FaxDefault nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkID nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDCategory nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDPrimaryContact nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	MailCity nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	MailCountry nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	MailPOBox nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	MailState nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	MailStreet nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	MailZipCode nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	Modified bit NULL,
	Name nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	OrderPrefix nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	PhoneDefault nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	POBox nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	State nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	Street nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	WebSiteDefault nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	ZipCode nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	ID uniqueidentifier DEFAULT newsequentialid() NOT NULL,
	[Type] int NULL,
	CONSTRAINT PK_GeneralContacts PRIMARY KEY (ID)
);
 CREATE NONCLUSTERED INDEX idxLinkID ON MicrovellumData.dbo.GeneralContacts (  LinkID ASC  )  
	 WITH (  PAD_INDEX = OFF ,FILLFACTOR = 100  ,SORT_IN_TEMPDB = OFF , IGNORE_DUP_KEY = OFF , STATISTICS_NORECOMPUTE = OFF , ONLINE = OFF , ALLOW_ROW_LOCKS = ON , ALLOW_PAGE_LOCKS = ON  )
	 ON [PRIMARY ] ;


-- MicrovellumData.dbo.GlobalFiles definition

-- Drop table

-- DROP TABLE MicrovellumData.dbo.GlobalFiles;

CREATE TABLE MicrovellumData.dbo.GlobalFiles (
	LinkID nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDCategory nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDLibrary nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDProject nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	Modified bit NULL,
	Name nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[Type] int NULL,
	WorkBook varbinary(MAX) NULL,
	ID uniqueidentifier DEFAULT newsequentialid() NOT NULL,
	CONSTRAINT PK_GlobalFiles PRIMARY KEY (ID)
);
 CREATE NONCLUSTERED INDEX idxLinkID ON MicrovellumData.dbo.GlobalFiles (  LinkID ASC  )  
	 WITH (  PAD_INDEX = OFF ,FILLFACTOR = 100  ,SORT_IN_TEMPDB = OFF , IGNORE_DUP_KEY = OFF , STATISTICS_NORECOMPUTE = OFF , ONLINE = OFF , ALLOW_ROW_LOCKS = ON , ALLOW_PAGE_LOCKS = ON  )
	 ON [PRIMARY ] ;


-- MicrovellumData.dbo.GlobalImages definition

-- Drop table

-- DROP TABLE MicrovellumData.dbo.GlobalImages;

CREATE TABLE MicrovellumData.dbo.GlobalImages (
	JPegName nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	JPegStream varbinary(MAX) NULL,
	LinkID nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDCategory nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	Modified bit NULL,
	Name nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	TiffName nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	TiffStream varbinary(MAX) NULL,
	WMFName nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	WMFStream varbinary(MAX) NULL,
	ID uniqueidentifier DEFAULT newsequentialid() NOT NULL,
	CONSTRAINT PK_GlobalImages PRIMARY KEY (ID)
);
 CREATE NONCLUSTERED INDEX idxLinkID ON MicrovellumData.dbo.GlobalImages (  LinkID ASC  )  
	 WITH (  PAD_INDEX = OFF ,FILLFACTOR = 100  ,SORT_IN_TEMPDB = OFF , IGNORE_DUP_KEY = OFF , STATISTICS_NORECOMPUTE = OFF , ONLINE = OFF , ALLOW_ROW_LOCKS = ON , ALLOW_PAGE_LOCKS = ON  )
	 ON [PRIMARY ] ;


-- MicrovellumData.dbo.Hardware definition

-- Drop table

-- DROP TABLE MicrovellumData.dbo.Hardware;

CREATE TABLE MicrovellumData.dbo.Hardware (
	BasePoint int NULL,
	BasePointX float NULL,
	BasePointY float NULL,
	BasePointZ float NULL,
	Code nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	Comments varchar(MAX) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	Comments1 varchar(MAX) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	Comments2 varchar(MAX) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	Comments3 varchar(MAX) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[Depth] float NULL,
	Grain int NULL,
	HatchType int NULL,
	Height float NULL,
	[Index] int NULL,
	InventoryAvailableQty float NULL,
	InventoryCurrentQty float NULL,
	InventoryMinQty float NULL,
	LinkID nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDBottomFaceRendering nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDCategory nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDCoreRendering nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDDefaultVendor nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDEQPart nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDMaterial nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDParentSubAssembly nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDProduct nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDProject nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDSheetSize nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDSubAssembly nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDTopFaceRendering nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDVendor nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDWorkOrder nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	MarkUp float NULL,
	Modified bit NULL,
	Name nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	PartType int NULL,
	Quantity float NULL,
	RotationX float NULL,
	RotationY float NULL,
	RotationZ float NULL,
	Thickness float NULL,
	TotalQuantity float NULL,
	[Type] int NULL,
	UnitType int NULL,
	VendorCost float NULL,
	WasteFactor float NULL,
	Width float NULL,
	ID uniqueidentifier DEFAULT newsequentialid() NOT NULL,
	HandlingCode nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	CodeFormula varchar(MAX) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	GrainFormula varchar(MAX) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	HandlingCodeFormula varchar(MAX) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	IsFormulaMaterial bit NULL,
	LinkIDProcessingStations varchar(MAX) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	MarkUpFormula varchar(MAX) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	MaterialCommentsFormula varchar(MAX) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	MaterialFlipSetting int NULL,
	ScanCode nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	WasteFactorFormula varchar(MAX) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	CONSTRAINT PK_Hardware PRIMARY KEY (ID)
);
 CREATE NONCLUSTERED INDEX idxLinkID ON MicrovellumData.dbo.Hardware (  LinkID ASC  )  
	 WITH (  PAD_INDEX = OFF ,FILLFACTOR = 100  ,SORT_IN_TEMPDB = OFF , IGNORE_DUP_KEY = OFF , STATISTICS_NORECOMPUTE = OFF , ONLINE = OFF , ALLOW_ROW_LOCKS = ON , ALLOW_PAGE_LOCKS = ON  )
	 ON [PRIMARY ] ;


-- MicrovellumData.dbo.HardwareFiles definition

-- Drop table

-- DROP TABLE MicrovellumData.dbo.HardwareFiles;

CREATE TABLE MicrovellumData.dbo.HardwareFiles (
	LinkID nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDCategory nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDLibrary nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDProject nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	Modified bit NULL,
	Name nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[Type] int NULL,
	WorkBook varbinary(MAX) NULL,
	ID uniqueidentifier DEFAULT newsequentialid() NOT NULL,
	CONSTRAINT PK_HardwareFiles PRIMARY KEY (ID)
);
 CREATE NONCLUSTERED INDEX idxLinkID ON MicrovellumData.dbo.HardwareFiles (  LinkID ASC  )  
	 WITH (  PAD_INDEX = OFF ,FILLFACTOR = 100  ,SORT_IN_TEMPDB = OFF , IGNORE_DUP_KEY = OFF , STATISTICS_NORECOMPUTE = OFF , ONLINE = OFF , ALLOW_ROW_LOCKS = ON , ALLOW_PAGE_LOCKS = ON  )
	 ON [PRIMARY ] ;


-- MicrovellumData.dbo.HardwareProcessingStations definition

-- Drop table

-- DROP TABLE MicrovellumData.dbo.HardwareProcessingStations;

CREATE TABLE MicrovellumData.dbo.HardwareProcessingStations (
	LinkID nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDBatch nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDCategory nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDHardware nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDProcessingStation nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDWorkOrder nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	Modified bit NULL,
	Name nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	ID uniqueidentifier DEFAULT newsequentialid() NOT NULL,
	CONSTRAINT PK_HardwareProcessingStations PRIMARY KEY (ID)
);
 CREATE NONCLUSTERED INDEX idxLinkID ON MicrovellumData.dbo.HardwareProcessingStations (  LinkID ASC  )  
	 WITH (  PAD_INDEX = OFF ,FILLFACTOR = 100  ,SORT_IN_TEMPDB = OFF , IGNORE_DUP_KEY = OFF , STATISTICS_NORECOMPUTE = OFF , ONLINE = OFF , ALLOW_ROW_LOCKS = ON , ALLOW_PAGE_LOCKS = ON  )
	 ON [PRIMARY ] ;


-- MicrovellumData.dbo.Holidays definition

-- Drop table

-- DROP TABLE MicrovellumData.dbo.Holidays;

CREATE TABLE MicrovellumData.dbo.Holidays (
	Comments nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	DateType int NULL,
	[Day] int NULL,
	EndDate datetime NULL,
	LinkID nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDCategory nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDUser nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	Modified bit NULL,
	[Month] int NULL,
	Name nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	StartDate datetime NULL,
	Week int NULL,
	WeekDay int NULL,
	ID uniqueidentifier DEFAULT newsequentialid() NOT NULL,
	MoveWeekend bit NULL,
	Observed bit NULL,
	PaidHoliday bit NULL,
	CONSTRAINT PK_Holidays PRIMARY KEY (ID)
);
 CREATE NONCLUSTERED INDEX idxLinkID ON MicrovellumData.dbo.Holidays (  LinkID ASC  )  
	 WITH (  PAD_INDEX = OFF ,FILLFACTOR = 100  ,SORT_IN_TEMPDB = OFF , IGNORE_DUP_KEY = OFF , STATISTICS_NORECOMPUTE = OFF , ONLINE = OFF , ALLOW_ROW_LOCKS = ON , ALLOW_PAGE_LOCKS = ON  )
	 ON [PRIMARY ] ;


-- MicrovellumData.dbo.Inventory definition

-- Drop table

-- DROP TABLE MicrovellumData.dbo.Inventory;

CREATE TABLE MicrovellumData.dbo.Inventory (
	InventoryLevel float NULL,
	InventoryMinimum float NULL,
	LinkID nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDCategory nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDParent nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	Modified bit NULL,
	Name nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[Type] int NULL,
	ID uniqueidentifier DEFAULT newsequentialid() NOT NULL,
	CONSTRAINT PK_Inventory PRIMARY KEY (ID)
);
 CREATE NONCLUSTERED INDEX idxLinkID ON MicrovellumData.dbo.Inventory (  LinkID ASC  )  
	 WITH (  PAD_INDEX = OFF ,FILLFACTOR = 100  ,SORT_IN_TEMPDB = OFF , IGNORE_DUP_KEY = OFF , STATISTICS_NORECOMPUTE = OFF , ONLINE = OFF , ALLOW_ROW_LOCKS = ON , ALLOW_PAGE_LOCKS = ON  )
	 ON [PRIMARY ] ;


-- MicrovellumData.dbo.JoinProducts definition

-- Drop table

-- DROP TABLE MicrovellumData.dbo.JoinProducts;

CREATE TABLE MicrovellumData.dbo.JoinProducts (
	LinkID nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDCategory nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDLibrary nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	Modified bit NULL,
	Name nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	ProductStarterName nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	SubassemblyName nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	ID uniqueidentifier DEFAULT newsequentialid() NOT NULL,
	CONSTRAINT PK_JoinProducts PRIMARY KEY (ID)
);
 CREATE NONCLUSTERED INDEX idxLinkID ON MicrovellumData.dbo.JoinProducts (  LinkID ASC  )  
	 WITH (  PAD_INDEX = OFF ,FILLFACTOR = 100  ,SORT_IN_TEMPDB = OFF , IGNORE_DUP_KEY = OFF , STATISTICS_NORECOMPUTE = OFF , ONLINE = OFF , ALLOW_ROW_LOCKS = ON , ALLOW_PAGE_LOCKS = ON  )
	 ON [PRIMARY ] ;


-- MicrovellumData.dbo.LabelSettings definition

-- Drop table

-- DROP TABLE MicrovellumData.dbo.LabelSettings;

CREATE TABLE MicrovellumData.dbo.LabelSettings (
	AllowRotation bit NULL,
	CenterDeviation float NULL,
	LinkID nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDCategory nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	MarginBorder float NULL,
	MarginMachining float NULL,
	Modified bit NULL,
	Name nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	PlacementType int NULL,
	ShowLocationsInNest bit NULL,
	ID uniqueidentifier DEFAULT newsequentialid() NOT NULL,
	LabelPrinterHeadOrientation int NULL,
	CONSTRAINT PK_LabelSettings PRIMARY KEY (ID)
);
 CREATE NONCLUSTERED INDEX idxLinkID ON MicrovellumData.dbo.LabelSettings (  LinkID ASC  )  
	 WITH (  PAD_INDEX = OFF ,FILLFACTOR = 100  ,SORT_IN_TEMPDB = OFF , IGNORE_DUP_KEY = OFF , STATISTICS_NORECOMPUTE = OFF , ONLINE = OFF , ALLOW_ROW_LOCKS = ON , ALLOW_PAGE_LOCKS = ON  )
	 ON [PRIMARY ] ;


-- MicrovellumData.dbo.Layers definition

-- Drop table

-- DROP TABLE MicrovellumData.dbo.Layers;

CREATE TABLE MicrovellumData.dbo.Layers (
	LinkID nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDCategory nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	Modified bit NULL,
	Name nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	PartName nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	ID uniqueidentifier DEFAULT newsequentialid() NOT NULL,
	RenderLayerType int NULL,
	SavedGroupName nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	SavedRenderMaterialName nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	CONSTRAINT PK_Layers PRIMARY KEY (ID)
);
 CREATE NONCLUSTERED INDEX idxLinkID ON MicrovellumData.dbo.Layers (  LinkID ASC  )  
	 WITH (  PAD_INDEX = OFF ,FILLFACTOR = 100  ,SORT_IN_TEMPDB = OFF , IGNORE_DUP_KEY = OFF , STATISTICS_NORECOMPUTE = OFF , ONLINE = OFF , ALLOW_ROW_LOCKS = ON , ALLOW_PAGE_LOCKS = ON  )
	 ON [PRIMARY ] ;


-- MicrovellumData.dbo.Libraries definition

-- Drop table

-- DROP TABLE MicrovellumData.dbo.Libraries;

CREATE TABLE MicrovellumData.dbo.Libraries (
	LinkID nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDCategory nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDTemplate nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	Modified bit NULL,
	Name nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[Type] int NULL,
	ID uniqueidentifier DEFAULT newsequentialid() NOT NULL,
	LastLibraryRestore nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	UpdateFlag int NULL,
	CONSTRAINT PK_Libraries PRIMARY KEY (ID)
);
 CREATE NONCLUSTERED INDEX idxLinkID ON MicrovellumData.dbo.Libraries (  LinkID ASC  )  
	 WITH (  PAD_INDEX = OFF ,FILLFACTOR = 100  ,SORT_IN_TEMPDB = OFF , IGNORE_DUP_KEY = OFF , STATISTICS_NORECOMPUTE = OFF , ONLINE = OFF , ALLOW_ROW_LOCKS = ON , ALLOW_PAGE_LOCKS = ON  )
	 ON [PRIMARY ] ;


-- MicrovellumData.dbo.Locations definition

-- Drop table

-- DROP TABLE MicrovellumData.dbo.Locations;

CREATE TABLE MicrovellumData.dbo.Locations (
	CategoryLevel int NULL,
	DrawingName nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkID nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDCategory nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDLocation nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDLocationCopy nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDProject nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	Modified bit NULL,
	Name nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	PrintFlag bit NULL,
	ID uniqueidentifier DEFAULT newsequentialid() NOT NULL,
	DesignFileSyncDate datetime NULL,
	JPegName nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	JPegStream varbinary(MAX) NULL,
	TiffName nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	TiffStream varbinary(MAX) NULL,
	WMFName nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	WMFStream varbinary(MAX) NULL,
	CONSTRAINT PK_Locations PRIMARY KEY (ID)
);
 CREATE NONCLUSTERED INDEX idxLinkID ON MicrovellumData.dbo.Locations (  LinkID ASC  )  
	 WITH (  PAD_INDEX = OFF ,FILLFACTOR = 100  ,SORT_IN_TEMPDB = OFF , IGNORE_DUP_KEY = OFF , STATISTICS_NORECOMPUTE = OFF , ONLINE = OFF , ALLOW_ROW_LOCKS = ON , ALLOW_PAGE_LOCKS = ON  )
	 ON [PRIMARY ] ;


-- MicrovellumData.dbo.MachiningTokenImages definition

-- Drop table

-- DROP TABLE MicrovellumData.dbo.MachiningTokenImages;

CREATE TABLE MicrovellumData.dbo.MachiningTokenImages (
	JPegName nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	JPegStream varbinary(MAX) NULL,
	LinkID nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDCategory nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	Modified bit NULL,
	Name nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	TiffName nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	TiffStream varbinary(MAX) NULL,
	WMFName nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	WMFStream varbinary(MAX) NULL,
	ID uniqueidentifier DEFAULT newsequentialid() NOT NULL,
	CONSTRAINT PK_MachiningTokenImages PRIMARY KEY (ID)
);
 CREATE NONCLUSTERED INDEX idxLinkID ON MicrovellumData.dbo.MachiningTokenImages (  LinkID ASC  )  
	 WITH (  PAD_INDEX = OFF ,FILLFACTOR = 100  ,SORT_IN_TEMPDB = OFF , IGNORE_DUP_KEY = OFF , STATISTICS_NORECOMPUTE = OFF , ONLINE = OFF , ALLOW_ROW_LOCKS = ON , ALLOW_PAGE_LOCKS = ON  )
	 ON [PRIMARY ] ;


-- MicrovellumData.dbo.Macros definition

-- Drop table

-- DROP TABLE MicrovellumData.dbo.Macros;

CREATE TABLE MicrovellumData.dbo.Macros (
	LinkID nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDCategory nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	Modified bit NULL,
	Name nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	WorkBook varbinary(MAX) NULL,
	ID uniqueidentifier DEFAULT newsequentialid() NOT NULL,
	CONSTRAINT PK_Macros PRIMARY KEY (ID)
);
 CREATE NONCLUSTERED INDEX idxLinkID ON MicrovellumData.dbo.Macros (  LinkID ASC  )  
	 WITH (  PAD_INDEX = OFF ,FILLFACTOR = 100  ,SORT_IN_TEMPDB = OFF , IGNORE_DUP_KEY = OFF , STATISTICS_NORECOMPUTE = OFF , ONLINE = OFF , ALLOW_ROW_LOCKS = ON , ALLOW_PAGE_LOCKS = ON  )
	 ON [PRIMARY ] ;


-- MicrovellumData.dbo.MaterialCheckout definition

-- Drop table

-- DROP TABLE MicrovellumData.dbo.MaterialCheckout;

CREATE TABLE MicrovellumData.dbo.MaterialCheckout (
	CheckoutDate datetime NULL,
	Comments nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkID nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDCategory nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDEmployee nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDReceivedMaterial nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDWorkOrder nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	Modified bit NULL,
	Name nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	Quantity float NULL,
	ID uniqueidentifier DEFAULT newsequentialid() NOT NULL,
	CONSTRAINT PK_MaterialCheckout PRIMARY KEY (ID)
);
 CREATE NONCLUSTERED INDEX idxLinkID ON MicrovellumData.dbo.MaterialCheckout (  LinkID ASC  )  
	 WITH (  PAD_INDEX = OFF ,FILLFACTOR = 100  ,SORT_IN_TEMPDB = OFF , IGNORE_DUP_KEY = OFF , STATISTICS_NORECOMPUTE = OFF , ONLINE = OFF , ALLOW_ROW_LOCKS = ON , ALLOW_PAGE_LOCKS = ON  )
	 ON [PRIMARY ] ;


-- MicrovellumData.dbo.MaterialCompositions definition

-- Drop table

-- DROP TABLE MicrovellumData.dbo.MaterialCompositions;

CREATE TABLE MicrovellumData.dbo.MaterialCompositions (
	Code nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	Comments nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	Grain int NULL,
	HatchType int NULL,
	IsDefault bit NULL,
	LinkID nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDBottomFaceRendering nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDCategory nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDCoreRendering nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDDefaultVendor nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDMaterialClass nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDSheetSize nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDTopFaceRendering nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	MarkUp float NULL,
	Modified bit NULL,
	Name nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	Quantity float NULL,
	StandardPrice float NULL,
	Thickness float NULL,
	[Type] int NULL,
	UnitType int NULL,
	WasteFactor float NULL,
	ID uniqueidentifier DEFAULT newsequentialid() NOT NULL,
	HandlingCode nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	CodeFormula varchar(MAX) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	GrainFormula varchar(MAX) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	HandlingCodeFormula varchar(MAX) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	IsFormulaMaterial bit NULL,
	MarkUpFormula varchar(MAX) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	MaterialCommentsFormula varchar(MAX) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	WasteFactorFormula varchar(MAX) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	MaterialFlipSetting int NULL,
	MaterialLaborValue float NULL,
	MaterialLaborValueFormula varchar(MAX) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	MaterialXData1 varchar(MAX) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	MaterialXData1Formula varchar(MAX) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	MaterialXData2 varchar(MAX) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	MaterialXData2Formula varchar(MAX) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	MaterialXData3 varchar(MAX) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	MaterialXData3Formula varchar(MAX) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	Region int NULL,
	SkipSpreadsheetSync bit NULL,
	CONSTRAINT PK_MaterialCompositions PRIMARY KEY (ID)
);
 CREATE NONCLUSTERED INDEX idxLinkID ON MicrovellumData.dbo.MaterialCompositions (  LinkID ASC  )  
	 WITH (  PAD_INDEX = OFF ,FILLFACTOR = 100  ,SORT_IN_TEMPDB = OFF , IGNORE_DUP_KEY = OFF , STATISTICS_NORECOMPUTE = OFF , ONLINE = OFF , ALLOW_ROW_LOCKS = ON , ALLOW_PAGE_LOCKS = ON  )
	 ON [PRIMARY ] ;


-- MicrovellumData.dbo.MaterialCompositionsRealMaterials definition

-- Drop table

-- DROP TABLE MicrovellumData.dbo.MaterialCompositionsRealMaterials;

CREATE TABLE MicrovellumData.dbo.MaterialCompositionsRealMaterials (
	LinkIDMaterialComposition nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDRealMaterial nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	ID uniqueidentifier DEFAULT newsequentialid() NOT NULL,
	CONSTRAINT PK_MaterialCompositionsRealMaterials PRIMARY KEY (ID)
);
 CREATE NONCLUSTERED INDEX idxLinkIDMaterialComposition ON MicrovellumData.dbo.MaterialCompositionsRealMaterials (  LinkIDMaterialComposition ASC  )  
	 WITH (  PAD_INDEX = OFF ,FILLFACTOR = 100  ,SORT_IN_TEMPDB = OFF , IGNORE_DUP_KEY = OFF , STATISTICS_NORECOMPUTE = OFF , ONLINE = OFF , ALLOW_ROW_LOCKS = ON , ALLOW_PAGE_LOCKS = ON  )
	 ON [PRIMARY ] ;
 CREATE NONCLUSTERED INDEX idxLinkIDRealMaterial ON MicrovellumData.dbo.MaterialCompositionsRealMaterials (  LinkIDRealMaterial ASC  )  
	 WITH (  PAD_INDEX = OFF ,FILLFACTOR = 100  ,SORT_IN_TEMPDB = OFF , IGNORE_DUP_KEY = OFF , STATISTICS_NORECOMPUTE = OFF , ONLINE = OFF , ALLOW_ROW_LOCKS = ON , ALLOW_PAGE_LOCKS = ON  )
	 ON [PRIMARY ] ;


-- MicrovellumData.dbo.MaterialCostBreaks definition

-- Drop table

-- DROP TABLE MicrovellumData.dbo.MaterialCostBreaks;

CREATE TABLE MicrovellumData.dbo.MaterialCostBreaks (
	Comments nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	DefaultEstimateCostBreak bit NULL,
	LinkID nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDCategory nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDMaterial nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDSheet nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDVendor nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	MaxQty float NULL,
	MinQty float NULL,
	Modified bit NULL,
	Name nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	SecondLevelConversion float NULL,
	SecondLevelUnit nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	ThirdLevelConversion float NULL,
	ThirdLevelUnit nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[Type] int NULL,
	UnitCost float NULL,
	UnitPrice float NULL,
	VendorMaterialCode nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	WaitTime float NULL,
	ID uniqueidentifier DEFAULT newsequentialid() NOT NULL,
	CONSTRAINT PK_MaterialCostBreaks PRIMARY KEY (ID)
);
 CREATE NONCLUSTERED INDEX idxLinkID ON MicrovellumData.dbo.MaterialCostBreaks (  LinkID ASC  )  
	 WITH (  PAD_INDEX = OFF ,FILLFACTOR = 100  ,SORT_IN_TEMPDB = OFF , IGNORE_DUP_KEY = OFF , STATISTICS_NORECOMPUTE = OFF , ONLINE = OFF , ALLOW_ROW_LOCKS = ON , ALLOW_PAGE_LOCKS = ON  )
	 ON [PRIMARY ] ;
 CREATE NONCLUSTERED INDEX lookup_idx ON MicrovellumData.dbo.MaterialCostBreaks (  LinkIDMaterial ASC  )  
	 WITH (  PAD_INDEX = OFF ,FILLFACTOR = 100  ,SORT_IN_TEMPDB = OFF , IGNORE_DUP_KEY = OFF , STATISTICS_NORECOMPUTE = OFF , ONLINE = OFF , ALLOW_ROW_LOCKS = ON , ALLOW_PAGE_LOCKS = ON  )
	 ON [PRIMARY ] ;


-- MicrovellumData.dbo.MaterialRendering definition

-- Drop table

-- DROP TABLE MicrovellumData.dbo.MaterialRendering;

CREATE TABLE MicrovellumData.dbo.MaterialRendering (
	LinkID nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDCategory nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	Modified bit NULL,
	Name nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	PictureName nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	RGBNumber nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	ID uniqueidentifier DEFAULT newsequentialid() NOT NULL,
	CONSTRAINT PK_MaterialRendering PRIMARY KEY (ID)
);
 CREATE NONCLUSTERED INDEX idxLinkID ON MicrovellumData.dbo.MaterialRendering (  LinkID ASC  )  
	 WITH (  PAD_INDEX = OFF ,FILLFACTOR = 100  ,SORT_IN_TEMPDB = OFF , IGNORE_DUP_KEY = OFF , STATISTICS_NORECOMPUTE = OFF , ONLINE = OFF , ALLOW_ROW_LOCKS = ON , ALLOW_PAGE_LOCKS = ON  )
	 ON [PRIMARY ] ;


-- MicrovellumData.dbo.MaterialStorageLocations definition

-- Drop table

-- DROP TABLE MicrovellumData.dbo.MaterialStorageLocations;

CREATE TABLE MicrovellumData.dbo.MaterialStorageLocations (
	Comments nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkID nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDCategory nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	Modified bit NULL,
	Name nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	PrintFlag bit NULL,
	ID uniqueidentifier DEFAULT newsequentialid() NOT NULL,
	ScanCode nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	UserDefinedID nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	CONSTRAINT PK_MaterialStorageLocations PRIMARY KEY (ID)
);
 CREATE NONCLUSTERED INDEX idxLinkID ON MicrovellumData.dbo.MaterialStorageLocations (  LinkID ASC  )  
	 WITH (  PAD_INDEX = OFF ,FILLFACTOR = 100  ,SORT_IN_TEMPDB = OFF , IGNORE_DUP_KEY = OFF , STATISTICS_NORECOMPUTE = OFF , ONLINE = OFF , ALLOW_ROW_LOCKS = ON , ALLOW_PAGE_LOCKS = ON  )
	 ON [PRIMARY ] ;


-- MicrovellumData.dbo.Materials definition

-- Drop table

-- DROP TABLE MicrovellumData.dbo.Materials;

CREATE TABLE MicrovellumData.dbo.Materials (
	Cloneable bit NULL,
	Code nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	Comments nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	CustomSizeAdjustment float NULL,
	ElvBlockFileName nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	Grain int NULL,
	HatchType int NULL,
	InventoryAvailableQty float NULL,
	InventoryCurrentQty float NULL,
	InventoryMinQty float NULL,
	InventoryValue float NULL,
	LinkID nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDBottomFaceRendering nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDCategory nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDCoreRendering nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDDefaultVendor nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDMaterialStorageLocation nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDProject nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDSheetSize nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDTopFaceRendering nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	MarkUp float NULL,
	MaterialEstimateCost float NULL,
	Modified bit NULL,
	Name nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	NonassociativeDrawingName nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	Quantity float NULL,
	Thickness float NULL,
	[Type] int NULL,
	UnitType int NULL,
	WasteFactor float NULL,
	ID uniqueidentifier DEFAULT newsequentialid() NOT NULL,
	StandardPrice float NULL,
	HandlingCode nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	CodeFormula varchar(MAX) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	CustomSizeAdjustmentFormula varchar(MAX) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	FormulaMaterialAlias nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	GrainFormula varchar(MAX) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	HandlingCodeFormula varchar(MAX) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	HatchName nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	IsFormulaMaterial bit NULL,
	MarkUpFormula varchar(MAX) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	MaterialCommentsFormula varchar(MAX) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	MaterialEstimateCostFormula varchar(MAX) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	NameFormula varchar(MAX) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	ThicknessFormula varchar(MAX) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	WasteFactorFormula varchar(MAX) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	MaterialFlipSetting int NULL,
	AssociativeDrawingName nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	AssociativeDrawingNameFormula nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	ElvBlockFileNameFormula nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	MaterialLaborValue float NULL,
	MaterialLaborValueFormula varchar(MAX) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	MaterialXData1 varchar(MAX) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	MaterialXData1Formula varchar(MAX) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	MaterialXData2 varchar(MAX) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	MaterialXData2Formula varchar(MAX) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	MaterialXData3 varchar(MAX) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	MaterialXData3Formula varchar(MAX) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	NonassociativeDrawingNameFormula nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	Region int NULL,
	SkipSpreadsheetSync bit NULL,
	CONSTRAINT Materials_PK PRIMARY KEY (ID)
);
 CREATE NONCLUSTERED INDEX idxLinkID ON MicrovellumData.dbo.Materials (  LinkID ASC  )  
	 WITH (  PAD_INDEX = OFF ,FILLFACTOR = 100  ,SORT_IN_TEMPDB = OFF , IGNORE_DUP_KEY = OFF , STATISTICS_NORECOMPUTE = OFF , ONLINE = OFF , ALLOW_ROW_LOCKS = ON , ALLOW_PAGE_LOCKS = ON  )
	 ON [PRIMARY ] ;


-- MicrovellumData.dbo.MaterialsVendors definition

-- Drop table

-- DROP TABLE MicrovellumData.dbo.MaterialsVendors;

CREATE TABLE MicrovellumData.dbo.MaterialsVendors (
	LinkIDMaterial nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDVendor nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	ID uniqueidentifier DEFAULT newsequentialid() NOT NULL,
	CONSTRAINT PK_MaterialsVendors PRIMARY KEY (ID)
);
 CREATE NONCLUSTERED INDEX idxLinkIDMaterial ON MicrovellumData.dbo.MaterialsVendors (  LinkIDMaterial ASC  )  
	 WITH (  PAD_INDEX = OFF ,FILLFACTOR = 100  ,SORT_IN_TEMPDB = OFF , IGNORE_DUP_KEY = OFF , STATISTICS_NORECOMPUTE = OFF , ONLINE = OFF , ALLOW_ROW_LOCKS = ON , ALLOW_PAGE_LOCKS = ON  )
	 ON [PRIMARY ] ;
 CREATE NONCLUSTERED INDEX idxLinkIDVendor ON MicrovellumData.dbo.MaterialsVendors (  LinkIDVendor ASC  )  
	 WITH (  PAD_INDEX = OFF ,FILLFACTOR = 100  ,SORT_IN_TEMPDB = OFF , IGNORE_DUP_KEY = OFF , STATISTICS_NORECOMPUTE = OFF , ONLINE = OFF , ALLOW_ROW_LOCKS = ON , ALLOW_PAGE_LOCKS = ON  )
	 ON [PRIMARY ] ;


-- MicrovellumData.dbo.MicrovellumSystem definition

-- Drop table

-- DROP TABLE MicrovellumData.dbo.MicrovellumSystem;

CREATE TABLE MicrovellumData.dbo.MicrovellumSystem (
	ApplicationVersion nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkID nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	Modified bit NULL,
	Name nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	ID uniqueidentifier DEFAULT newsequentialid() NOT NULL,
	BundleSeed int NULL,
	ServerQueueLocation nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	StorageGroupSeed int NULL,
	DefaultMaterialFlipValue int NULL,
	EventLogCount int NULL,
	ScrapIDSeed int NULL,
	CONSTRAINT PK_MicrovellumSystem PRIMARY KEY (ID)
);
 CREATE NONCLUSTERED INDEX idxLinkID ON MicrovellumData.dbo.MicrovellumSystem (  LinkID ASC  )  
	 WITH (  PAD_INDEX = OFF ,FILLFACTOR = 100  ,SORT_IN_TEMPDB = OFF , IGNORE_DUP_KEY = OFF , STATISTICS_NORECOMPUTE = OFF , ONLINE = OFF , ALLOW_ROW_LOCKS = ON , ALLOW_PAGE_LOCKS = ON  )
	 ON [PRIMARY ] ;


-- MicrovellumData.dbo.NestScrap definition

-- Drop table

-- DROP TABLE MicrovellumData.dbo.NestScrap;

CREATE TABLE MicrovellumData.dbo.NestScrap (
	Grain int NULL,
	IDScrap int NULL,
	[Length] float NULL,
	LinkID nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDCategory nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDNestScrapBin nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDWorkOrderBatch nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	MaterialCode nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	MaterialName nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	Modified bit NULL,
	Name nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	Quantity int NULL,
	Thickness float NULL,
	Width float NULL,
	ID uniqueidentifier DEFAULT newsequentialid() NOT NULL,
	CONSTRAINT PK_NestScrap PRIMARY KEY (ID)
);
 CREATE NONCLUSTERED INDEX idxLinkID ON MicrovellumData.dbo.NestScrap (  LinkID ASC  )  
	 WITH (  PAD_INDEX = OFF ,FILLFACTOR = 100  ,SORT_IN_TEMPDB = OFF , IGNORE_DUP_KEY = OFF , STATISTICS_NORECOMPUTE = OFF , ONLINE = OFF , ALLOW_ROW_LOCKS = ON , ALLOW_PAGE_LOCKS = ON  )
	 ON [PRIMARY ] ;


-- MicrovellumData.dbo.NestScrapBin definition

-- Drop table

-- DROP TABLE MicrovellumData.dbo.NestScrapBin;

CREATE TABLE MicrovellumData.dbo.NestScrapBin (
	Description nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkID nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDCategory nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	Modified bit NULL,
	Name nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	ID uniqueidentifier DEFAULT newsequentialid() NOT NULL,
	CONSTRAINT PK_NestScrapBin PRIMARY KEY (ID)
);
 CREATE NONCLUSTERED INDEX idxLinkID ON MicrovellumData.dbo.NestScrapBin (  LinkID ASC  )  
	 WITH (  PAD_INDEX = OFF ,FILLFACTOR = 100  ,SORT_IN_TEMPDB = OFF , IGNORE_DUP_KEY = OFF , STATISTICS_NORECOMPUTE = OFF , ONLINE = OFF , ALLOW_ROW_LOCKS = ON , ALLOW_PAGE_LOCKS = ON  )
	 ON [PRIMARY ] ;


-- MicrovellumData.dbo.NestingOptimizerSettings definition

-- Drop table

-- DROP TABLE MicrovellumData.dbo.NestingOptimizerSettings;

CREATE TABLE MicrovellumData.dbo.NestingOptimizerSettings (
	FavorItemNumbers bit NULL,
	GroupPartsWithFace6Machining bit NULL,
	LastFileName nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LastNestFileName nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkID nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDCategory nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	MaxQTYPerSheet int NULL,
	Modified bit NULL,
	Name nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	RouterBitDiameter float NULL,
	ID uniqueidentifier DEFAULT newsequentialid() NOT NULL,
	AlwaysDoSecondPass bit NULL,
	BorderSequence int NULL,
	CompositeNestType int NULL,
	DistanceBetweenParts float NULL,
	DontRouteSecondPass bit NULL,
	IrregularPartThreshold float NULL,
	NestBorderRoutingOptimizationType int NULL,
	NestFromLeft bit NULL,
	NestFromTop bit NULL,
	NestOptimizationStrategy int NULL,
	NestPartGroupingType int NULL,
	NestRampLength float NULL,
	NestRampOffset float NULL,
	NestSetMillLocation int NULL,
	NestTextFormat int NULL,
	NoRotateDia float NULL,
	NoRotateQty int NULL,
	RestrictPlacementByDrillReach bit NULL,
	RunFace6NestFirst bit NULL,
	SmallPartArea float NULL,
	SmallPartDimension float NULL,
	StayDownComplexity int NULL,
	TabHeight float NULL,
	TabLength1 float NULL,
	TabLength2 float NULL,
	ThicknessLeftForFirstPass float NULL,
	SmallPartHandlingType int NULL,
	TabAllowRemain bit NULL,
	TabPlaceQuantity int NULL,
	TabTool nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	SmallPartsBuffer int NULL,
	TabSheetTrim float NULL,
	CreateCompositeJpg bit NULL,
	NoRotateDiaSecondary float NULL,
	NoRotateQtySecondary int NULL,
	OptimizerTimeAllowance int NULL,
	ParallelRouteRestriction float NULL,
	PartRotationRestrictionEnabled bit NULL,
	PrimaryGroup int NULL,
	SecondaryGroup int NULL,
	SmallPartsToCenterEnabled bit NULL,
	TertiaryGroup int NULL,
	TrueShapeEnabled bit NULL,
	StayDownAvoidSharedConnectors bit NULL,
	StayDownDoFlatSConnections bit NULL,
	StayDownPlaceSetMillOnSheetsEdge bit NULL,
	OptimizerPersistence int NULL,
	SheetSelectionStrategy int NULL,
	CONSTRAINT PK_NestingOptimizerSettings PRIMARY KEY (ID)
);
 CREATE NONCLUSTERED INDEX idxLinkID ON MicrovellumData.dbo.NestingOptimizerSettings (  LinkID ASC  )  
	 WITH (  PAD_INDEX = OFF ,FILLFACTOR = 100  ,SORT_IN_TEMPDB = OFF , IGNORE_DUP_KEY = OFF , STATISTICS_NORECOMPUTE = OFF , ONLINE = OFF , ALLOW_ROW_LOCKS = ON , ALLOW_PAGE_LOCKS = ON  )
	 ON [PRIMARY ] ;


-- MicrovellumData.dbo.OptimizationResults definition

-- Drop table

-- DROP TABLE MicrovellumData.dbo.OptimizationResults;

CREATE TABLE MicrovellumData.dbo.OptimizationResults (
	AlreadyPrinted bit NULL,
	BorderCoordinates varbinary(MAX) NULL,
	EBBottomRotated nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	EBLeftRotated nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	EBRightRotated nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	EBTopRotated nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	EdgeBoreBottomRotated nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	EdgeBoreLeftRotated nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	EdgeBoreRightRotated nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	EdgeBoreTopRotated nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	Face5Barcode nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	Face5FileName nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	Face6Barcode nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	Face6FileName nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	FullFace6FileName nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	FullFileName nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[Index] int NULL,
	IsScrap bit NULL,
	JPegStream varbinary(MAX) NULL,
	LabelImageName nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[Length] float NULL,
	LinkID nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDChildSheet nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDMaterial nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDPart nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkidProcessingStation nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDSheet nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDStack nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDStrip nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDWorkOrder nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDWorkOrderBatch nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	Name nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	OptimizedQuantity int NULL,
	OptimizerFlipped bit NULL,
	OptimizerRotated bit NULL,
	OptimizerRotationAngle float NULL,
	PrintFlag bit NULL,
	PrintFlags nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	ScrapName nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	ScrapType int NULL,
	TiffStream varbinary(MAX) NULL,
	[Type] int NULL,
	Width float NULL,
	WMFName nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	WMFNameFace6 nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	WMFStream varbinary(MAX) NULL,
	WMFStreamDimensioned varbinary(MAX) NULL,
	WMFStreamFace6 varbinary(MAX) NULL,
	WMFStreamFace6Dimensioned varbinary(MAX) NULL,
	XCoord float NULL,
	YCoord float NULL,
	ID uniqueidentifier DEFAULT newsequentialid() NOT NULL,
	BorderCoordinatesString varchar(MAX) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LabelRotation float NULL,
	LabelX float NULL,
	LabelY float NULL,
	CONSTRAINT PK_OptimizationResults PRIMARY KEY (ID)
);
 CREATE NONCLUSTERED INDEX idxLinkID ON MicrovellumData.dbo.OptimizationResults (  LinkID ASC  )  
	 WITH (  PAD_INDEX = OFF ,FILLFACTOR = 100  ,SORT_IN_TEMPDB = OFF , IGNORE_DUP_KEY = OFF , STATISTICS_NORECOMPUTE = OFF , ONLINE = OFF , ALLOW_ROW_LOCKS = ON , ALLOW_PAGE_LOCKS = ON  )
	 ON [PRIMARY ] ;


-- MicrovellumData.dbo.OrderLineItems definition

-- Drop table

-- DROP TABLE MicrovellumData.dbo.OrderLineItems;

CREATE TABLE MicrovellumData.dbo.OrderLineItems (
	Active bit NULL,
	AdjustedUnitPrice float NULL,
	BaseUnitPrice float NULL,
	Comments nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	CustomTypeName nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	DateCreated datetime NULL,
	DateDelivered datetime NULL,
	DateInvoiced datetime NULL,
	Discount float NULL,
	EstimatedUnitPrice float NULL,
	LineNumber int NULL,
	LineTotal float NULL,
	LinkID nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDCategory nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDChildSalesOrder nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDCreatorContact nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDMaterial nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDParent nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDProduct nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	Modified bit NULL,
	Name nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	Premium float NULL,
	PreviousStatus int NULL,
	ProductSKU nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	Quantity float NULL,
	Status int NULL,
	[Type] int NULL,
	UnitOfMeasure int NULL,
	ID uniqueidentifier DEFAULT newsequentialid() NOT NULL,
	CONSTRAINT PK_OrderLineItems PRIMARY KEY (ID)
);
 CREATE NONCLUSTERED INDEX idxLinkID ON MicrovellumData.dbo.OrderLineItems (  LinkID ASC  )  
	 WITH (  PAD_INDEX = OFF ,FILLFACTOR = 100  ,SORT_IN_TEMPDB = OFF , IGNORE_DUP_KEY = OFF , STATISTICS_NORECOMPUTE = OFF , ONLINE = OFF , ALLOW_ROW_LOCKS = ON , ALLOW_PAGE_LOCKS = ON  )
	 ON [PRIMARY ] ;


-- MicrovellumData.dbo.Pallets definition

-- Drop table

-- DROP TABLE MicrovellumData.dbo.Pallets;

CREATE TABLE MicrovellumData.dbo.Pallets (
	Comments nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	DateCreated datetime NULL,
	LinkID nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDCategory nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDEmployee nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDLocation nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDShippingTicket nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	Modified bit NULL,
	Name nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	PalletNumber int NULL,
	PrintFlag bit NULL,
	ID uniqueidentifier DEFAULT newsequentialid() NOT NULL,
	CONSTRAINT PK_Pallets PRIMARY KEY (ID)
);
 CREATE NONCLUSTERED INDEX idxLinkID ON MicrovellumData.dbo.Pallets (  LinkID ASC  )  
	 WITH (  PAD_INDEX = OFF ,FILLFACTOR = 100  ,SORT_IN_TEMPDB = OFF , IGNORE_DUP_KEY = OFF , STATISTICS_NORECOMPUTE = OFF , ONLINE = OFF , ALLOW_ROW_LOCKS = ON , ALLOW_PAGE_LOCKS = ON  )
	 ON [PRIMARY ] ;


-- MicrovellumData.dbo.PalletsProducts definition

-- Drop table

-- DROP TABLE MicrovellumData.dbo.PalletsProducts;

CREATE TABLE MicrovellumData.dbo.PalletsProducts (
	LinkIDPallet nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDProduct nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	ID uniqueidentifier DEFAULT newsequentialid() NOT NULL,
	CONSTRAINT PK_PalletsProducts PRIMARY KEY (ID)
);
 CREATE NONCLUSTERED INDEX idxLinkIDPallet ON MicrovellumData.dbo.PalletsProducts (  LinkIDPallet ASC  )  
	 WITH (  PAD_INDEX = OFF ,FILLFACTOR = 100  ,SORT_IN_TEMPDB = OFF , IGNORE_DUP_KEY = OFF , STATISTICS_NORECOMPUTE = OFF , ONLINE = OFF , ALLOW_ROW_LOCKS = ON , ALLOW_PAGE_LOCKS = ON  )
	 ON [PRIMARY ] ;
 CREATE NONCLUSTERED INDEX idxLinkIDProduct ON MicrovellumData.dbo.PalletsProducts (  LinkIDProduct ASC  )  
	 WITH (  PAD_INDEX = OFF ,FILLFACTOR = 100  ,SORT_IN_TEMPDB = OFF , IGNORE_DUP_KEY = OFF , STATISTICS_NORECOMPUTE = OFF , ONLINE = OFF , ALLOW_ROW_LOCKS = ON , ALLOW_PAGE_LOCKS = ON  )
	 ON [PRIMARY ] ;


-- MicrovellumData.dbo.ParentStations definition

-- Drop table

-- DROP TABLE MicrovellumData.dbo.ParentStations;

CREATE TABLE MicrovellumData.dbo.ParentStations (
	LinkID nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDCategory nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	Modified bit NULL,
	Name nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	ID uniqueidentifier DEFAULT newsequentialid() NOT NULL,
	CONSTRAINT PK_ParentStations PRIMARY KEY (ID)
);
 CREATE NONCLUSTERED INDEX idxLinkID ON MicrovellumData.dbo.ParentStations (  LinkID ASC  )  
	 WITH (  PAD_INDEX = OFF ,FILLFACTOR = 100  ,SORT_IN_TEMPDB = OFF , IGNORE_DUP_KEY = OFF , STATISTICS_NORECOMPUTE = OFF , ONLINE = OFF , ALLOW_ROW_LOCKS = ON , ALLOW_PAGE_LOCKS = ON  )
	 ON [PRIMARY ] ;


-- MicrovellumData.dbo.PartSettings definition

-- Drop table

-- DROP TABLE MicrovellumData.dbo.PartSettings;

CREATE TABLE MicrovellumData.dbo.PartSettings (
	LinkID nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDCategory nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	Modified bit NULL,
	Name nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	RotationType int NULL,
	ID uniqueidentifier DEFAULT newsequentialid() NOT NULL,
	BorderRampLength float NULL,
	BorderRampOffset float NULL,
	DXF3DBorders bit NULL,
	RemoveLeads bit NULL,
	CONSTRAINT PK_PartSettings PRIMARY KEY (ID)
);
 CREATE NONCLUSTERED INDEX idxLinkID ON MicrovellumData.dbo.PartSettings (  LinkID ASC  )  
	 WITH (  PAD_INDEX = OFF ,FILLFACTOR = 100  ,SORT_IN_TEMPDB = OFF , IGNORE_DUP_KEY = OFF , STATISTICS_NORECOMPUTE = OFF , ONLINE = OFF , ALLOW_ROW_LOCKS = ON , ALLOW_PAGE_LOCKS = ON  )
	 ON [PRIMARY ] ;


-- MicrovellumData.dbo.Parts definition

-- Drop table

-- DROP TABLE MicrovellumData.dbo.Parts;

CREATE TABLE MicrovellumData.dbo.Parts (
	AdjustedCutPartLength float NULL,
	AdjustedCutPartWidth float NULL,
	Barcode nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	BasePoint int NULL,
	BasePointX float NULL,
	BasePointY float NULL,
	BasePointZ float NULL,
	Code nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	Comments varchar(MAX) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	Comments1 varchar(MAX) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	Comments2 varchar(MAX) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	Comments3 varchar(MAX) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	CutPartLength float NULL,
	CutPartWidth float NULL,
	DontIncludeRoutesinNestBorder bit NULL,
	DrawToken2DElv nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	DrawToken3D nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	DXFFileName nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	EdgeNameBottom nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	EdgeNameBottomWMF nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	EdgeNameLeft nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	EdgeNameLeftWMF nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	EdgeNameRight nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	EdgeNameRightWMF nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	EdgeNameTop nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	EdgeNameTopWMF nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	Face6Barcode nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	Face6FileName nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	FileName nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	FullFace6FileName nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	FullFileName nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	Grain int NULL,
	HatchType int NULL,
	HboreBarcodeLeft nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	HboreBarcodeLower nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	HboreBarcodeRight nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	HboreBarcodeUpper nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	HboreFileNameLeft nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	HboreFileNameLower nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	HboreFileNameRight nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	HboreFileNameUpper nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[Index] int NULL,
	InventoryAvailableQty float NULL,
	InventoryCurrentQty float NULL,
	InventoryMinQty float NULL,
	JPegStream varbinary(MAX) NULL,
	[Length] float NULL,
	LinkID nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDBottomFaceRendering nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDCategory nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDCoreRendering nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDDefaultVendor nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDEQPart nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDMaterial nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDParentSubAssembly nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDProcessingStations varchar(MAX) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDProduct nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDProject nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDSheetSize nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDSubAssembly nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDTopFaceRendering nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDVendor nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDWorkOrder nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	MachinePoint nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	MarkUp float NULL,
	MaterialCode nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	MaterialName nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	MaterialThickness float NULL,
	MaterialType int NULL,
	Modified bit NULL,
	Name nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	OverridePartCutLength float NULL,
	OverridePartCutThickness float NULL,
	OverridePartCutWidth float NULL,
	Par1 nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	Par2 nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	Par3 nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	PartType int NULL,
	PerfectGrainCaption nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	PrintFlag bit NULL,
	Processed bit NULL,
	Quantity float NULL,
	RotationX float NULL,
	RotationY float NULL,
	RotationZ float NULL,
	Row_ID int NULL,
	RunFieldNameFace5 nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	RunFieldNameFace6 nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	Thickness float NULL,
	TiffStream varbinary(MAX) NULL,
	TotalQuantity float NULL,
	[Type] int NULL,
	UnitType int NULL,
	VendorCost float NULL,
	WasteFactor float NULL,
	Width float NULL,
	WMFName nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	WMFNameFace6 nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	WMFStream varbinary(MAX) NULL,
	WMFStreamDimensioned varbinary(MAX) NULL,
	WMFStreamFace6 varbinary(MAX) NULL,
	WMFStreamFace6Dimensioned varbinary(MAX) NULL,
	ID uniqueidentifier DEFAULT newsequentialid() NOT NULL,
	HandlingCode nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	MaterialComments nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	EdgeSequence nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	FinishPriority nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	IrregularShape nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	Location1 nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	Location2 nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	UDID nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	XD01 nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	XD02 nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	XD03 nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	XD04 nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	XD05 nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	XD06 nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	XD07 nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	XD08 nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	XD09 nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	XD10 nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	XD11 nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	XD12 nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	XD13 nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	XD14 nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	XD15 nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	XD16 nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	XD17 nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	XD18 nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	CodeFormula varchar(MAX) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	GrainFormula varchar(MAX) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	HandlingCodeFormula varchar(MAX) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	IsFormulaMaterial bit NULL,
	LabelPosition nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	MarkUpFormula varchar(MAX) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	MaterialCommentsFormula varchar(MAX) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	MaterialFlipSetting int NULL,
	ScanCode nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	WasteFactorFormula varchar(MAX) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	CONSTRAINT PK_Parts PRIMARY KEY (ID)
);
 CREATE NONCLUSTERED INDEX idxLinkID ON MicrovellumData.dbo.Parts (  LinkID ASC  )  
	 WITH (  PAD_INDEX = OFF ,FILLFACTOR = 100  ,SORT_IN_TEMPDB = OFF , IGNORE_DUP_KEY = OFF , STATISTICS_NORECOMPUTE = OFF , ONLINE = OFF , ALLOW_ROW_LOCKS = ON , ALLOW_PAGE_LOCKS = ON  )
	 ON [PRIMARY ] ;


-- MicrovellumData.dbo.PartsProcessingStations definition

-- Drop table

-- DROP TABLE MicrovellumData.dbo.PartsProcessingStations;

CREATE TABLE MicrovellumData.dbo.PartsProcessingStations (
	LinkIDBatch nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDPart nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDProcessingStation nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDWorkOrder nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	ID uniqueidentifier DEFAULT newsequentialid() NOT NULL,
	CONSTRAINT PK_PartsProcessingStations PRIMARY KEY (ID)
);


-- MicrovellumData.dbo.PlacedSheets definition

-- Drop table

-- DROP TABLE MicrovellumData.dbo.PlacedSheets;

CREATE TABLE MicrovellumData.dbo.PlacedSheets (
	BarCode nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	Code nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	Comments nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	CPOUTNumber int NULL,
	Face6BarCode nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	Face6FileName nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	FileName nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	FullFace6FileName nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	FullFileName nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	Grain int NULL,
	HatchType int NULL,
	[Index] int NULL,
	InventoryAvailableQty float NULL,
	InventoryCurrentQty float NULL,
	InventoryMinQty float NULL,
	JPegStream varbinary(MAX) NULL,
	LeadingLengthTrim float NULL,
	LeadingWidthTrim float NULL,
	[Length] float NULL,
	LinkID nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDBottomFaceRendering nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDCategory nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDCoreRendering nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDDefaultVendor nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDMaterial nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDMaterialStorageLocation nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDProcessingStation nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDSheetSize nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDStack nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDSubType nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDTopFaceRendering nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDWorkOrder nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDWorkOrderBatch nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	MarkUp float NULL,
	MaterialCost float NULL,
	Modified bit NULL,
	Name nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	OptimizationPriority int NULL,
	OptimizationType int NULL,
	Quantity float NULL,
	Scrap nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	ScrapType int NULL,
	Thickness float NULL,
	TiffStream varbinary(MAX) NULL,
	TrailingLengthTrim float NULL,
	TrailingWidthTrim float NULL,
	[Type] int NULL,
	UnitType int NULL,
	WasteFactor float NULL,
	Width float NULL,
	WMFStream varbinary(MAX) NULL,
	WMFStreamFace6 varbinary(MAX) NULL,
	ID uniqueidentifier DEFAULT newsequentialid() NOT NULL,
	CPOUTName nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	HandlingCode nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	CodeFormula varchar(MAX) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	GrainFormula varchar(MAX) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	HandlingCodeFormula varchar(MAX) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	IsFormulaMaterial bit NULL,
	LeadingLengthTrimFormula varchar(MAX) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LeadingWidthTrimFormula varchar(MAX) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LengthFormula varchar(MAX) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	MarkUpFormula varchar(MAX) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	MaterialCommentsFormula varchar(MAX) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	MaterialCostFormula varchar(MAX) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	MaterialFlipSetting int NULL,
	OptimizationPriorityFormula varchar(MAX) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	QtyFormula varchar(MAX) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	SawFileName nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	SpredsheetColumn int NULL,
	TrailingLengthTrimFormula varchar(MAX) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	TrailingWidthTrimFormula varchar(MAX) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	WasteFactorFormula varchar(MAX) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	WidthFormula varchar(MAX) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	Yield float NULL,
	CONSTRAINT PK_PlacedSheets PRIMARY KEY (ID)
);
 CREATE NONCLUSTERED INDEX idxLinkID ON MicrovellumData.dbo.PlacedSheets (  LinkID ASC  )  
	 WITH (  PAD_INDEX = OFF ,FILLFACTOR = 100  ,SORT_IN_TEMPDB = OFF , IGNORE_DUP_KEY = OFF , STATISTICS_NORECOMPUTE = OFF , ONLINE = OFF , ALLOW_ROW_LOCKS = ON , ALLOW_PAGE_LOCKS = ON  )
	 ON [PRIMARY ] ;


-- MicrovellumData.dbo.PlacedSheetsVendors definition

-- Drop table

-- DROP TABLE MicrovellumData.dbo.PlacedSheetsVendors;

CREATE TABLE MicrovellumData.dbo.PlacedSheetsVendors (
	LinkIDMaterial nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDVendor nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDWorkOrder nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	ID uniqueidentifier DEFAULT newsequentialid() NOT NULL,
	CONSTRAINT PK_PlacedSheetsVendors PRIMARY KEY (ID)
);


-- MicrovellumData.dbo.ProcessingStationAssociates definition

-- Drop table

-- DROP TABLE MicrovellumData.dbo.ProcessingStationAssociates;

CREATE TABLE MicrovellumData.dbo.ProcessingStationAssociates (
	[Index] int NULL,
	LinkID nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDChild nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDParent nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[Type] int NULL,
	ID uniqueidentifier DEFAULT newsequentialid() NOT NULL,
	CONSTRAINT PK_ProcessingStationAssociates PRIMARY KEY (ID)
);
 CREATE NONCLUSTERED INDEX idxLinkID ON MicrovellumData.dbo.ProcessingStationAssociates (  LinkID ASC  )  
	 WITH (  PAD_INDEX = OFF ,FILLFACTOR = 100  ,SORT_IN_TEMPDB = OFF , IGNORE_DUP_KEY = OFF , STATISTICS_NORECOMPUTE = OFF , ONLINE = OFF , ALLOW_ROW_LOCKS = ON , ALLOW_PAGE_LOCKS = ON  )
	 ON [PRIMARY ] ;


-- MicrovellumData.dbo.ProcessingStations definition

-- Drop table

-- DROP TABLE MicrovellumData.dbo.ProcessingStations;

CREATE TABLE MicrovellumData.dbo.ProcessingStations (
	ApplyPartsAll bit NULL,
	ApplyPartsContainsFaceMachining bit NULL,
	ApplyPartsContainsHboreMachining bit NULL,
	ApplyPartsWithAnyMachining bit NULL,
	ApplyPartsWithHBoreMachining bit NULL,
	ApplyPartsWithNoMachining bit NULL,
	FolderDestination nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LabelImageDestination nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LabelImageSize int NULL,
	LabelImageType int NULL,
	LabelImageZoom float NULL,
	LinkID nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDCategory nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDSawOptimizationSetting nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDToolFile nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	Modified bit NULL,
	Name nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	PartPictureSize int NULL,
	PartPictureType int NULL,
	PartPictureZoom float NULL,
	PartSizeAdjustmentMode int NULL,
	[Type] int NULL,
	UseLegacyWMFImageType bit NULL,
	WMFFolderDestination nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	ID uniqueidentifier DEFAULT newsequentialid() NOT NULL,
	ScrapMinArea float NULL,
	ScrapMinDim float NULL,
	ScrapOverrideTrims bit NULL,
	ScrapSave bit NULL,
	ScrapTrimLeadingLength float NULL,
	ScrapTrimLeadingWidth float NULL,
	ScrapTrimTrailingLength float NULL,
	ScrapTrimTrailingWidth float NULL,
	ScrapUse bit NULL,
	LinkIDNestOptimizationSetting nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	CreateSubFolders bit NULL,
	LinkIDAutoLoadListSetting nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	ScrapDimAndLogic bit NULL,
	ApplyPartsByLocation bit NULL,
	ApplyPartsByMaterial bit NULL,
	ApplyPartsContainsFace6Machining bit NULL,
	ApplyPartsWithFace6Machining bit NULL,
	LinkIDBundleItemSetting nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDScrapOptimizationSetting nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDLabelSettings nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDPartSetting nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	Units int NULL,
	AutoAssignRulesOnWOCreation bit NULL,
	OptimizerPerformanceMaxTimePerTest int NULL,
	OptimizerPerformanceResultsLogPath nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	OptimizerPerformanceTestCount int NULL,
	OptimizerVersion int NULL,
	CONSTRAINT PK_ProcessingStations PRIMARY KEY (ID)
);
 CREATE NONCLUSTERED INDEX idxLinkID ON MicrovellumData.dbo.ProcessingStations (  LinkID ASC  )  
	 WITH (  PAD_INDEX = OFF ,FILLFACTOR = 100  ,SORT_IN_TEMPDB = OFF , IGNORE_DUP_KEY = OFF , STATISTICS_NORECOMPUTE = OFF , ONLINE = OFF , ALLOW_ROW_LOCKS = ON , ALLOW_PAGE_LOCKS = ON  )
	 ON [PRIMARY ] ;


-- MicrovellumData.dbo.ProductCosts definition

-- Drop table

-- DROP TABLE MicrovellumData.dbo.ProductCosts;

CREATE TABLE MicrovellumData.dbo.ProductCosts (
	LinkID nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDCategory nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDLocation nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDProduct nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDProject nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	Modified bit NULL,
	Name nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	ProductCostAdjustment float NULL,
	ProductCostEngineering float NULL,
	ProductCostEngineeringMarkup float NULL,
	ProductCostInstall float NULL,
	ProductCostInstallMarkup float NULL,
	ProductCostInstallTax float NULL,
	ProductCostLabor float NULL,
	ProductCostLaborMarkup float NULL,
	ProductCostLaborTax float NULL,
	ProductCostLaborTotal float NULL,
	ProductCostMaterial float NULL,
	ProductCostMaterialMarkup float NULL,
	ProductCostMaterialTax float NULL,
	ProductCostMaterialTotal float NULL,
	ProductCostOverhead float NULL,
	ProductCostOverheadMarkup float NULL,
	ProductCostProfit float NULL,
	ProductCostProfitMarkup float NULL,
	ProductCostShipping float NULL,
	ProductCostShippingMarkup float NULL,
	ProductCostShippingTax float NULL,
	ProductCostTime float NULL,
	ProductCostTotal float NULL,
	ID uniqueidentifier DEFAULT newsequentialid() NOT NULL,
	CONSTRAINT PK_ProductCosts PRIMARY KEY (ID)
);
 CREATE NONCLUSTERED INDEX idxLinkID ON MicrovellumData.dbo.ProductCosts (  LinkID ASC  )  
	 WITH (  PAD_INDEX = OFF ,FILLFACTOR = 100  ,SORT_IN_TEMPDB = OFF , IGNORE_DUP_KEY = OFF , STATISTICS_NORECOMPUTE = OFF , ONLINE = OFF , ALLOW_ROW_LOCKS = ON , ALLOW_PAGE_LOCKS = ON  )
	 ON [PRIMARY ] ;


-- MicrovellumData.dbo.ProductGroups definition

-- Drop table

-- DROP TABLE MicrovellumData.dbo.ProductGroups;

CREATE TABLE MicrovellumData.dbo.ProductGroups (
	LinkID nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDCategory nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	Modified bit NULL,
	Name nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	X float NULL,
	Y float NULL,
	Z float NULL,
	ID uniqueidentifier DEFAULT newsequentialid() NOT NULL,
	JPegName nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	JPegStream varbinary(MAX) NULL,
	TiffName nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	TiffStream varbinary(MAX) NULL,
	WMFName nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	WMFStream varbinary(MAX) NULL,
	CONSTRAINT PK_ProductGroups PRIMARY KEY (ID)
);
 CREATE NONCLUSTERED INDEX idxLinkID ON MicrovellumData.dbo.ProductGroups (  LinkID ASC  )  
	 WITH (  PAD_INDEX = OFF ,FILLFACTOR = 100  ,SORT_IN_TEMPDB = OFF , IGNORE_DUP_KEY = OFF , STATISTICS_NORECOMPUTE = OFF , ONLINE = OFF , ALLOW_ROW_LOCKS = ON , ALLOW_PAGE_LOCKS = ON  )
	 ON [PRIMARY ] ;


-- MicrovellumData.dbo.ProductMap definition

-- Drop table

-- DROP TABLE MicrovellumData.dbo.ProductMap;

CREATE TABLE MicrovellumData.dbo.ProductMap (
	[Depth] float NULL,
	Height float NULL,
	LinkID nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDCategory nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDLibrary nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDProduct nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	Modified bit NULL,
	Name nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	Width float NULL,
	ID uniqueidentifier DEFAULT newsequentialid() NOT NULL,
	CONSTRAINT PK_ProductMap PRIMARY KEY (ID)
);
 CREATE NONCLUSTERED INDEX idxLinkID ON MicrovellumData.dbo.ProductMap (  LinkID ASC  )  
	 WITH (  PAD_INDEX = OFF ,FILLFACTOR = 100  ,SORT_IN_TEMPDB = OFF , IGNORE_DUP_KEY = OFF , STATISTICS_NORECOMPUTE = OFF , ONLINE = OFF , ALLOW_ROW_LOCKS = ON , ALLOW_PAGE_LOCKS = ON  )
	 ON [PRIMARY ] ;


-- MicrovellumData.dbo.ProductPrices definition

-- Drop table

-- DROP TABLE MicrovellumData.dbo.ProductPrices;

CREATE TABLE MicrovellumData.dbo.ProductPrices (
	LinkID nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDCategory nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDLocation nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDProduct nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDProject nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	Modified bit NULL,
	Name nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	ProductPrice float NULL,
	ProductPriceAdjustment float NULL,
	ProductPriceEngineering float NULL,
	ProductPriceEngineeringMarkup float NULL,
	ProductPriceInstall float NULL,
	ProductPriceInstallMarkup float NULL,
	ProductPriceInstallTax float NULL,
	ProductPriceManufacturingTax float NULL,
	ProductPriceMarkup float NULL,
	ProductPriceOverhead float NULL,
	ProductPriceOverheadMarkup float NULL,
	ProductPriceShipping float NULL,
	ProductPriceShippingMarkup float NULL,
	ProductPriceShippingTax float NULL,
	ProductPriceTotal float NULL,
	ID uniqueidentifier DEFAULT newsequentialid() NOT NULL,
	CONSTRAINT PK_ProductPrices PRIMARY KEY (ID)
);
 CREATE NONCLUSTERED INDEX idxLinkID ON MicrovellumData.dbo.ProductPrices (  LinkID ASC  )  
	 WITH (  PAD_INDEX = OFF ,FILLFACTOR = 100  ,SORT_IN_TEMPDB = OFF , IGNORE_DUP_KEY = OFF , STATISTICS_NORECOMPUTE = OFF , ONLINE = OFF , ALLOW_ROW_LOCKS = ON , ALLOW_PAGE_LOCKS = ON  )
	 ON [PRIMARY ] ;


-- MicrovellumData.dbo.ProductPromptImages definition

-- Drop table

-- DROP TABLE MicrovellumData.dbo.ProductPromptImages;

CREATE TABLE MicrovellumData.dbo.ProductPromptImages (
	JPegName nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	JPegStream varbinary(MAX) NULL,
	LinkID nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDCategory nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	Modified bit NULL,
	Name nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	TiffName nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	TiffStream varbinary(MAX) NULL,
	[Type] int NULL,
	WMFName nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	WMFStream varbinary(MAX) NULL,
	ID uniqueidentifier DEFAULT newsequentialid() NOT NULL,
	CONSTRAINT PK_ProductPromptImages PRIMARY KEY (ID)
);
 CREATE NONCLUSTERED INDEX idxLinkID ON MicrovellumData.dbo.ProductPromptImages (  LinkID ASC  )  
	 WITH (  PAD_INDEX = OFF ,FILLFACTOR = 100  ,SORT_IN_TEMPDB = OFF , IGNORE_DUP_KEY = OFF , STATISTICS_NORECOMPUTE = OFF , ONLINE = OFF , ALLOW_ROW_LOCKS = ON , ALLOW_PAGE_LOCKS = ON  )
	 ON [PRIMARY ] ;


-- MicrovellumData.dbo.Products definition

-- Drop table

-- DROP TABLE MicrovellumData.dbo.Products;

CREATE TABLE MicrovellumData.dbo.Products (
	ActivityPath nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	ActivityPathShort nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	AncorType int NULL,
	Angle float NULL,
	Comments nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	Comments1 nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	Comments2 nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	Comments3 nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	CopiedLinkIDList nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	DateShipped datetime NULL,
	[Depth] float NULL,
	DoPerfectGrain bit NULL,
	DrawIndex int NULL,
	Extruded bit NULL,
	Height float NULL,
	IsBuyOut bit NULL,
	ItemNumber nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	JPegName nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	JPegStream varbinary(MAX) NULL,
	LinkID nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDCategory nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDLibrary nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDLocation nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDProductGroup nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDProject nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDRelease nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDSpecificationGroup nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDWall nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDWorkOrder nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	Modified bit NULL,
	Name nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	PerfectGrainChar nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	PrintFlag bit NULL,
	Quantity int NULL,
	QuantityShipped int NULL,
	QuoteName nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	RoomComponentType int NULL,
	RoomName nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	Row_ID int NULL,
	ShippingTicketName nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	TiffName nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	TiffStream varbinary(MAX) NULL,
	[Type] int NULL,
	UITreeFilter int NULL,
	Width float NULL,
	WMFName nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	WMFStream varbinary(MAX) NULL,
	WorkBook varbinary(MAX) NULL,
	WorkOrderName nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	X float NULL,
	Y float NULL,
	Z float NULL,
	ID uniqueidentifier DEFAULT newsequentialid() NOT NULL,
	ProductSpecGroupName nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	PrintFlags nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	ScanCode nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	CONSTRAINT PK_Products PRIMARY KEY (ID)
);
 CREATE NONCLUSTERED INDEX idxLinkID ON MicrovellumData.dbo.Products (  LinkID ASC  )  
	 WITH (  PAD_INDEX = OFF ,FILLFACTOR = 100  ,SORT_IN_TEMPDB = OFF , IGNORE_DUP_KEY = OFF , STATISTICS_NORECOMPUTE = OFF , ONLINE = OFF , ALLOW_ROW_LOCKS = ON , ALLOW_PAGE_LOCKS = ON  )
	 ON [PRIMARY ] ;


-- MicrovellumData.dbo.ProductsPrompts definition

-- Drop table

-- DROP TABLE MicrovellumData.dbo.ProductsPrompts;

CREATE TABLE MicrovellumData.dbo.ProductsPrompts (
	LinkIDProduct nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDPrompt nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDSubassembly nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDWorkOrder nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	ID uniqueidentifier DEFAULT newsequentialid() NOT NULL,
	CONSTRAINT PK_ProductsPrompts PRIMARY KEY (ID)
);


-- MicrovellumData.dbo.ProjectProperties definition

-- Drop table

-- DROP TABLE MicrovellumData.dbo.ProjectProperties;

CREATE TABLE MicrovellumData.dbo.ProjectProperties (
	GlobalVariableName nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkID nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDCategory nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDProject nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	Modified bit NULL,
	Name nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	ProjectVariableName nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	ProjectWizardVariableName nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	PropertyName nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	PropertyValue nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	SpecificationGroupName nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	TitleBlockAttributeName nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	TitleBlockName nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	ID uniqueidentifier DEFAULT newsequentialid() NOT NULL,
	LinkIDLocation nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	CONSTRAINT PK_ProjectProperties PRIMARY KEY (ID)
);
 CREATE NONCLUSTERED INDEX idxLinkID ON MicrovellumData.dbo.ProjectProperties (  LinkID ASC  )  
	 WITH (  PAD_INDEX = OFF ,FILLFACTOR = 100  ,SORT_IN_TEMPDB = OFF , IGNORE_DUP_KEY = OFF , STATISTICS_NORECOMPUTE = OFF , ONLINE = OFF , ALLOW_ROW_LOCKS = ON , ALLOW_PAGE_LOCKS = ON  )
	 ON [PRIMARY ] ;


-- MicrovellumData.dbo.ProjectWizardFiles definition

-- Drop table

-- DROP TABLE MicrovellumData.dbo.ProjectWizardFiles;

CREATE TABLE MicrovellumData.dbo.ProjectWizardFiles (
	LinkID nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDCategory nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDLibrary nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDProject nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	Modified bit NULL,
	Name nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[Type] int NULL,
	WorkBook varbinary(MAX) NULL,
	ID uniqueidentifier DEFAULT newsequentialid() NOT NULL,
	CONSTRAINT PK_ProjectWizardFiles PRIMARY KEY (ID)
);
 CREATE NONCLUSTERED INDEX idxLinkID ON MicrovellumData.dbo.ProjectWizardFiles (  LinkID ASC  )  
	 WITH (  PAD_INDEX = OFF ,FILLFACTOR = 100  ,SORT_IN_TEMPDB = OFF , IGNORE_DUP_KEY = OFF , STATISTICS_NORECOMPUTE = OFF , ONLINE = OFF , ALLOW_ROW_LOCKS = ON , ALLOW_PAGE_LOCKS = ON  )
	 ON [PRIMARY ] ;


-- MicrovellumData.dbo.ProjectWorkOrderActivities definition

-- Drop table

-- DROP TABLE MicrovellumData.dbo.ProjectWorkOrderActivities;

CREATE TABLE MicrovellumData.dbo.ProjectWorkOrderActivities (
	ActualValue float NULL,
	LinkID nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDCategory nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDProduct nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDProject nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDWorkOrder nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDWorkOrderActivity nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	Modified bit NULL,
	Name nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	ScheduledValue float NULL,
	ID uniqueidentifier DEFAULT newsequentialid() NOT NULL,
	CONSTRAINT PK_ProjectWorkOrderActivities PRIMARY KEY (ID)
);
 CREATE NONCLUSTERED INDEX idxLinkID ON MicrovellumData.dbo.ProjectWorkOrderActivities (  LinkID ASC  )  
	 WITH (  PAD_INDEX = OFF ,FILLFACTOR = 100  ,SORT_IN_TEMPDB = OFF , IGNORE_DUP_KEY = OFF , STATISTICS_NORECOMPUTE = OFF , ONLINE = OFF , ALLOW_ROW_LOCKS = ON , ALLOW_PAGE_LOCKS = ON  )
	 ON [PRIMARY ] ;


-- MicrovellumData.dbo.Projects definition

-- Drop table

-- DROP TABLE MicrovellumData.dbo.Projects;

CREATE TABLE MicrovellumData.dbo.Projects (
	Architect nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	Contractor nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	Draftsman nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	Estimator nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	GeneralContact nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	IsInactive bit NULL,
	JobAddress nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	JobDescription nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	JobEMail nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	JobFax nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	JobNumber nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	JobPhone nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkID nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDCategory nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDCustomerCompany nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LocationCoordinates nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	Modified bit NULL,
	Name nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	PrintFlag bit NULL,
	ProjectBudget float NULL,
	ProjectManager nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	ProjectNumber int NULL,
	ScheduledCompletionDate datetime NULL,
	ScheduledStartDate datetime NULL,
	TotalProjectCost float NULL,
	[Type] int NULL,
	ID uniqueidentifier DEFAULT newsequentialid() NOT NULL,
	DateCreated datetime NULL,
	DateLastOpened datetime NULL,
	CONSTRAINT PK_Projects PRIMARY KEY (ID)
);
 CREATE NONCLUSTERED INDEX idxLinkID ON MicrovellumData.dbo.Projects (  LinkID ASC  )  
	 WITH (  PAD_INDEX = OFF ,FILLFACTOR = 100  ,SORT_IN_TEMPDB = OFF , IGNORE_DUP_KEY = OFF , STATISTICS_NORECOMPUTE = OFF , ONLINE = OFF , ALLOW_ROW_LOCKS = ON , ALLOW_PAGE_LOCKS = ON  )
	 ON [PRIMARY ] ;
 CREATE NONCLUSTERED INDEX lookup_name_idx ON MicrovellumData.dbo.Projects (  Name ASC  )  
	 WITH (  PAD_INDEX = OFF ,FILLFACTOR = 100  ,SORT_IN_TEMPDB = OFF , IGNORE_DUP_KEY = OFF , STATISTICS_NORECOMPUTE = OFF , ONLINE = OFF , ALLOW_ROW_LOCKS = ON , ALLOW_PAGE_LOCKS = ON  )
	 ON [PRIMARY ] ;


-- MicrovellumData.dbo.PromptMap definition

-- Drop table

-- DROP TABLE MicrovellumData.dbo.PromptMap;

CREATE TABLE MicrovellumData.dbo.PromptMap (
	LinkID nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDCategory nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDLibrary nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDProduct nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	MappingName nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	Modified bit NULL,
	Name nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	ID uniqueidentifier DEFAULT newsequentialid() NOT NULL,
	CONSTRAINT PK_PromptMap PRIMARY KEY (ID)
);
 CREATE NONCLUSTERED INDEX idxLinkID ON MicrovellumData.dbo.PromptMap (  LinkID ASC  )  
	 WITH (  PAD_INDEX = OFF ,FILLFACTOR = 100  ,SORT_IN_TEMPDB = OFF , IGNORE_DUP_KEY = OFF , STATISTICS_NORECOMPUTE = OFF , ONLINE = OFF , ALLOW_ROW_LOCKS = ON , ALLOW_PAGE_LOCKS = ON  )
	 ON [PRIMARY ] ;


-- MicrovellumData.dbo.Prompts definition

-- Drop table

-- DROP TABLE MicrovellumData.dbo.Prompts;

CREATE TABLE MicrovellumData.dbo.Prompts (
	ControlType int NULL,
	HideOnReports bit NULL,
	IsQuantity bit NULL,
	LinkID nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDCategory nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDProduct nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDSubassembly nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDWorkOrder nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	Modified bit NULL,
	Name nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[Type] int NULL,
	Value nvarchar(MAX) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	ID uniqueidentifier DEFAULT newsequentialid() NOT NULL,
	CONSTRAINT PK_Prompts PRIMARY KEY (ID)
);
 CREATE NONCLUSTERED INDEX idxLinkID ON MicrovellumData.dbo.Prompts (  LinkID ASC  )  
	 WITH (  PAD_INDEX = OFF ,FILLFACTOR = 100  ,SORT_IN_TEMPDB = OFF , IGNORE_DUP_KEY = OFF , STATISTICS_NORECOMPUTE = OFF , ONLINE = OFF , ALLOW_ROW_LOCKS = ON , ALLOW_PAGE_LOCKS = ON  )
	 ON [PRIMARY ] ;


-- MicrovellumData.dbo.PurchaseOrders definition

-- Drop table

-- DROP TABLE MicrovellumData.dbo.PurchaseOrders;

CREATE TABLE MicrovellumData.dbo.PurchaseOrders (
	AccountNumber nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	BillToAddress nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	Comments nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	ConfirmationNumber nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	DateCreated datetime NULL,
	DateUpdated datetime NULL,
	DocumentActions int NULL,
	ExpectedArrivalDate datetime NULL,
	LinkID nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDCategory nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDEmployee nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDProject nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDUpdatingEmployee nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDVendor nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDVendorContact nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	Modified bit NULL,
	Name nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	Other float NULL,
	PoReport varbinary(MAX) NULL,
	PrintFlag bit NULL,
	PurchaseDate datetime NULL,
	PurchaseOrderNumber int NULL,
	ShippingAndHandling float NULL,
	ShipToAddress nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	Status int NULL,
	TaxPercentage float NULL,
	[Type] int NULL,
	ID uniqueidentifier DEFAULT newsequentialid() NOT NULL,
	CONSTRAINT PK_PurchaseOrders PRIMARY KEY (ID)
);
 CREATE NONCLUSTERED INDEX idxLinkID ON MicrovellumData.dbo.PurchaseOrders (  LinkID ASC  )  
	 WITH (  PAD_INDEX = OFF ,FILLFACTOR = 100  ,SORT_IN_TEMPDB = OFF , IGNORE_DUP_KEY = OFF , STATISTICS_NORECOMPUTE = OFF , ONLINE = OFF , ALLOW_ROW_LOCKS = ON , ALLOW_PAGE_LOCKS = ON  )
	 ON [PRIMARY ] ;


-- MicrovellumData.dbo.PurchasedMaterial definition

-- Drop table

-- DROP TABLE MicrovellumData.dbo.PurchasedMaterial;

CREATE TABLE MicrovellumData.dbo.PurchasedMaterial (
	Code nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	Cost float NULL,
	DateCreated datetime NULL,
	LinkID nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDCategory nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDCostBreak nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDMaterial nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDPart nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDProduct nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDProject nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDPurchaseOrder nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDSheet nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDWorkOrder nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	Modified bit NULL,
	Name nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	Quantity float NULL,
	QuantityAllocated float NULL,
	QuantityDamaged float NULL,
	QuantityOnBackOrder float NULL,
	QuantityOrdered float NULL,
	QuantityReceived float NULL,
	[Type] int NULL,
	UnitType int NULL,
	ID uniqueidentifier DEFAULT newsequentialid() NOT NULL,
	CONSTRAINT PK_PurchasedMaterial PRIMARY KEY (ID)
);
 CREATE NONCLUSTERED INDEX idxLinkID ON MicrovellumData.dbo.PurchasedMaterial (  LinkID ASC  )  
	 WITH (  PAD_INDEX = OFF ,FILLFACTOR = 100  ,SORT_IN_TEMPDB = OFF , IGNORE_DUP_KEY = OFF , STATISTICS_NORECOMPUTE = OFF , ONLINE = OFF , ALLOW_ROW_LOCKS = ON , ALLOW_PAGE_LOCKS = ON  )
	 ON [PRIMARY ] ;


-- MicrovellumData.dbo.RealMaterials definition

-- Drop table

-- DROP TABLE MicrovellumData.dbo.RealMaterials;

CREATE TABLE MicrovellumData.dbo.RealMaterials (
	Code nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	Comments nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	Grain int NULL,
	HatchType int NULL,
	LinkID nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDBottomFaceRendering nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDCategory nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDCoreRendering nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDDefaultVendor nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDSheetSize nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDTopFaceRendering nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	MarkUp float NULL,
	Modified bit NULL,
	Name nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	Quantity float NULL,
	SKU nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	StandardPrice float NULL,
	Thickness float NULL,
	[Type] int NULL,
	UnitType int NULL,
	WasteFactor float NULL,
	ID uniqueidentifier DEFAULT newsequentialid() NOT NULL,
	HandlingCode nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	CodeFormula varchar(MAX) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	GrainFormula varchar(MAX) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	HandlingCodeFormula varchar(MAX) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	IsFormulaMaterial bit NULL,
	MarkUpFormula varchar(MAX) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	MaterialCommentsFormula varchar(MAX) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	WasteFactorFormula varchar(MAX) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	MaterialFlipSetting int NULL,
	MaterialLaborValue float NULL,
	MaterialLaborValueFormula varchar(MAX) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	MaterialXData1 varchar(MAX) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	MaterialXData1Formula varchar(MAX) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	MaterialXData2 varchar(MAX) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	MaterialXData2Formula varchar(MAX) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	MaterialXData3 varchar(MAX) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	MaterialXData3Formula varchar(MAX) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	Region int NULL,
	SkipSpreadsheetSync bit NULL,
	CONSTRAINT PK_RealMaterials PRIMARY KEY (ID)
);
 CREATE NONCLUSTERED INDEX idxLinkID ON MicrovellumData.dbo.RealMaterials (  LinkID ASC  )  
	 WITH (  PAD_INDEX = OFF ,FILLFACTOR = 100  ,SORT_IN_TEMPDB = OFF , IGNORE_DUP_KEY = OFF , STATISTICS_NORECOMPUTE = OFF , ONLINE = OFF , ALLOW_ROW_LOCKS = ON , ALLOW_PAGE_LOCKS = ON  )
	 ON [PRIMARY ] ;


-- MicrovellumData.dbo.ReceivedMaterials definition

-- Drop table

-- DROP TABLE MicrovellumData.dbo.ReceivedMaterials;

CREATE TABLE MicrovellumData.dbo.ReceivedMaterials (
	Comments nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	InventoryValue float NULL,
	LinkID nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDCategory nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDEmployee nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDMaterial nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDMaterialStorageLocation nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDPurchasedMaterial nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDPurchaseOrder nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDSheet nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	Modified bit NULL,
	Name nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	PrintFlag bit NULL,
	PrintQuantity float NULL,
	Quantity float NULL,
	QuantityCheckedOut float NULL,
	ReceivedDate datetime NULL,
	Status int NULL,
	[Type] int NULL,
	ID uniqueidentifier DEFAULT newsequentialid() NOT NULL,
	CONSTRAINT PK_ReceivedMaterials PRIMARY KEY (ID)
);
 CREATE NONCLUSTERED INDEX idxLinkID ON MicrovellumData.dbo.ReceivedMaterials (  LinkID ASC  )  
	 WITH (  PAD_INDEX = OFF ,FILLFACTOR = 100  ,SORT_IN_TEMPDB = OFF , IGNORE_DUP_KEY = OFF , STATISTICS_NORECOMPUTE = OFF , ONLINE = OFF , ALLOW_ROW_LOCKS = ON , ALLOW_PAGE_LOCKS = ON  )
	 ON [PRIMARY ] ;


-- MicrovellumData.dbo.ReceivedMaterialsPurchasedMaterial definition

-- Drop table

-- DROP TABLE MicrovellumData.dbo.ReceivedMaterialsPurchasedMaterial;

CREATE TABLE MicrovellumData.dbo.ReceivedMaterialsPurchasedMaterial (
	LinkIDPurchasedMaterial nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDReceivedMaterial nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	ID uniqueidentifier DEFAULT newsequentialid() NOT NULL,
	CONSTRAINT PK_ReceivedMaterialsPurchasedMaterial PRIMARY KEY (ID)
);
 CREATE NONCLUSTERED INDEX idxLinkIDPurchasedMaterial ON MicrovellumData.dbo.ReceivedMaterialsPurchasedMaterial (  LinkIDPurchasedMaterial ASC  )  
	 WITH (  PAD_INDEX = OFF ,FILLFACTOR = 100  ,SORT_IN_TEMPDB = OFF , IGNORE_DUP_KEY = OFF , STATISTICS_NORECOMPUTE = OFF , ONLINE = OFF , ALLOW_ROW_LOCKS = ON , ALLOW_PAGE_LOCKS = ON  )
	 ON [PRIMARY ] ;
 CREATE NONCLUSTERED INDEX idxLinkIDReceivedMaterial ON MicrovellumData.dbo.ReceivedMaterialsPurchasedMaterial (  LinkIDReceivedMaterial ASC  )  
	 WITH (  PAD_INDEX = OFF ,FILLFACTOR = 100  ,SORT_IN_TEMPDB = OFF , IGNORE_DUP_KEY = OFF , STATISTICS_NORECOMPUTE = OFF , ONLINE = OFF , ALLOW_ROW_LOCKS = ON , ALLOW_PAGE_LOCKS = ON  )
	 ON [PRIMARY ] ;


-- MicrovellumData.dbo.ReportDatasets definition

-- Drop table

-- DROP TABLE MicrovellumData.dbo.ReportDatasets;

CREATE TABLE MicrovellumData.dbo.ReportDatasets (
	IncludedColumns nvarchar(MAX) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkID nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDCategory nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDReport nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	Modified bit NULL,
	Name nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	SqlString nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[Type] int NULL,
	ID uniqueidentifier DEFAULT newsequentialid() NOT NULL,
	CONSTRAINT PK_ReportDatasets PRIMARY KEY (ID)
);
 CREATE NONCLUSTERED INDEX idxLinkID ON MicrovellumData.dbo.ReportDatasets (  LinkID ASC  )  
	 WITH (  PAD_INDEX = OFF ,FILLFACTOR = 100  ,SORT_IN_TEMPDB = OFF , IGNORE_DUP_KEY = OFF , STATISTICS_NORECOMPUTE = OFF , ONLINE = OFF , ALLOW_ROW_LOCKS = ON , ALLOW_PAGE_LOCKS = ON  )
	 ON [PRIMARY ] ;


-- MicrovellumData.dbo.ReportExportSettings definition

-- Drop table

-- DROP TABLE MicrovellumData.dbo.ReportExportSettings;

CREATE TABLE MicrovellumData.dbo.ReportExportSettings (
	CSVBandFilter int NULL,
	CSVSeparator nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	CSVSkipColumnHeaders bit NULL,
	ExcelBandFilter int NULL,
	ExcelExportEachPage bit NULL,
	ExcelExportObjectFormatting bit NULL,
	ExcelExportPageBreaks bit NULL,
	ExcelOnePage bit NULL,
	ExcelType int NULL,
	ExportSettingType int NULL,
	HTMLAddPageBreaks bit NULL,
	HTMLCompressToArchive bit NULL,
	HTMLEmbeddedImageData bit NULL,
	HTMLExportMode int NULL,
	HTMLImageFormat int NULL,
	HTMLScale int NULL,
	ImageCutEdges bit NULL,
	ImageMonochromeType int NULL,
	ImageQuality int NULL,
	ImageResolution int NULL,
	ImageScale int NULL,
	ImageTIFFCompression int NULL,
	ImageTIFFMultipleFile bit NULL,
	ImageType int NULL,
	LinkID nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDCategory nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	Modified bit NULL,
	Name nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	PDFAllowCompliance bit NULL,
	PDFAllowEditable bit NULL,
	PDFCompliance int NULL,
	PDFEmbeddedFonts bit NULL,
	PDFExportRTAsImage bit NULL,
	PDFImageCompressionMethod int NULL,
	PDFImageResolution int NULL,
	PDFImageResolutionMode int NULL,
	RestrictEditing int NULL,
	WordRemoveEmptySpace bit NULL,
	WordUseHeadersAndFooters bit NULL,
	ID uniqueidentifier DEFAULT newsequentialid() NOT NULL,
	CONSTRAINT PK_ReportExportSettings PRIMARY KEY (ID)
);
 CREATE NONCLUSTERED INDEX idxLinkID ON MicrovellumData.dbo.ReportExportSettings (  LinkID ASC  )  
	 WITH (  PAD_INDEX = OFF ,FILLFACTOR = 100  ,SORT_IN_TEMPDB = OFF , IGNORE_DUP_KEY = OFF , STATISTICS_NORECOMPUTE = OFF , ONLINE = OFF , ALLOW_ROW_LOCKS = ON , ALLOW_PAGE_LOCKS = ON  )
	 ON [PRIMARY ] ;


-- MicrovellumData.dbo.ReportGroupItems definition

-- Drop table

-- DROP TABLE MicrovellumData.dbo.ReportGroupItems;

CREATE TABLE MicrovellumData.dbo.ReportGroupItems (
	LinkID nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDCategory nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDProcessingStation nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDReport nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDReportGroup nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	Modified bit NULL,
	Name nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	OutputPath nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	OutputType int NULL,
	ID uniqueidentifier DEFAULT newsequentialid() NOT NULL,
	CONSTRAINT PK_ReportGroupItems PRIMARY KEY (ID)
);
 CREATE NONCLUSTERED INDEX idxLinkID ON MicrovellumData.dbo.ReportGroupItems (  LinkID ASC  )  
	 WITH (  PAD_INDEX = OFF ,FILLFACTOR = 100  ,SORT_IN_TEMPDB = OFF , IGNORE_DUP_KEY = OFF , STATISTICS_NORECOMPUTE = OFF , ONLINE = OFF , ALLOW_ROW_LOCKS = ON , ALLOW_PAGE_LOCKS = ON  )
	 ON [PRIMARY ] ;


-- MicrovellumData.dbo.ReportGroups definition

-- Drop table

-- DROP TABLE MicrovellumData.dbo.ReportGroups;

CREATE TABLE MicrovellumData.dbo.ReportGroups (
	CreateReportDirectory bit NULL,
	LinkID nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDCategory nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	Modified bit NULL,
	Name nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	OutputNameFormat nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	OutputPath nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	RunGroupOnBatchComplete bit NULL,
	RunGroupOnWOComplete bit NULL,
	ID uniqueidentifier DEFAULT newsequentialid() NOT NULL,
	CONSTRAINT PK_ReportGroups PRIMARY KEY (ID)
);
 CREATE NONCLUSTERED INDEX idxLinkID ON MicrovellumData.dbo.ReportGroups (  LinkID ASC  )  
	 WITH (  PAD_INDEX = OFF ,FILLFACTOR = 100  ,SORT_IN_TEMPDB = OFF , IGNORE_DUP_KEY = OFF , STATISTICS_NORECOMPUTE = OFF , ONLINE = OFF , ALLOW_ROW_LOCKS = ON , ALLOW_PAGE_LOCKS = ON  )
	 ON [PRIMARY ] ;


-- MicrovellumData.dbo.ReportTemplates definition

-- Drop table

-- DROP TABLE MicrovellumData.dbo.ReportTemplates;

CREATE TABLE MicrovellumData.dbo.ReportTemplates (
	BusinessObjectTypes int NULL,
	DatabaseFileName nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	GlobalVariables nvarchar(MAX) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	HideInReportList bit NULL,
	IsBatchReport bit NULL,
	LinkID nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDCategory nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	Modified bit NULL,
	Name nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	ProductVariables nvarchar(MAX) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	ReportCategoryType int NULL,
	ReportFile varbinary(MAX) NULL,
	SqlString nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	ID uniqueidentifier DEFAULT newsequentialid() NOT NULL,
	Comments nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	ModifiedVersion nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	CONSTRAINT PK_ReportTemplates PRIMARY KEY (ID)
);
 CREATE NONCLUSTERED INDEX idxLinkID ON MicrovellumData.dbo.ReportTemplates (  LinkID ASC  )  
	 WITH (  PAD_INDEX = OFF ,FILLFACTOR = 100  ,SORT_IN_TEMPDB = OFF , IGNORE_DUP_KEY = OFF , STATISTICS_NORECOMPUTE = OFF , ONLINE = OFF , ALLOW_ROW_LOCKS = ON , ALLOW_PAGE_LOCKS = ON  )
	 ON [PRIMARY ] ;


-- MicrovellumData.dbo.SalesOrders definition

-- Drop table

-- DROP TABLE MicrovellumData.dbo.SalesOrders;

CREATE TABLE MicrovellumData.dbo.SalesOrders (
	Active bit NULL,
	Comments nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	DateClosed datetime NULL,
	DateConfirmed datetime NULL,
	DateCreated datetime NULL,
	DateDeleted datetime NULL,
	DateDeliveredActual datetime NULL,
	DateDeliveryPromised datetime NULL,
	DateInvoiceApproved datetime NULL,
	DateInvoiced datetime NULL,
	DatePaid datetime NULL,
	DateProductionFinish datetime NULL,
	DateProductionStart datetime NULL,
	DateRejected datetime NULL,
	DateReleased datetime NULL,
	DateShipped datetime NULL,
	InvoiceNumber nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkID nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDCategory nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDCreatorContact nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDCustomerCompany nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDProject nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	Modified bit NULL,
	Name nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	OrderNumber int NULL,
	OrderNumberPrefix nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	PoNumber nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	PreviousStatus int NULL,
	ShippingMethod int NULL,
	Status int NULL,
	TaxRate float NULL,
	[Type] int NULL,
	ID uniqueidentifier DEFAULT newsequentialid() NOT NULL,
	CONSTRAINT PK_SalesOrders PRIMARY KEY (ID)
);
 CREATE NONCLUSTERED INDEX idxLinkID ON MicrovellumData.dbo.SalesOrders (  LinkID ASC  )  
	 WITH (  PAD_INDEX = OFF ,FILLFACTOR = 100  ,SORT_IN_TEMPDB = OFF , IGNORE_DUP_KEY = OFF , STATISTICS_NORECOMPUTE = OFF , ONLINE = OFF , ALLOW_ROW_LOCKS = ON , ALLOW_PAGE_LOCKS = ON  )
	 ON [PRIMARY ] ;


-- MicrovellumData.dbo.SavedViews definition

-- Drop table

-- DROP TABLE MicrovellumData.dbo.SavedViews;

CREATE TABLE MicrovellumData.dbo.SavedViews (
	ColumnWidths nvarchar(MAX) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	DecimalPlaces int NULL,
	FontSize float NULL,
	GridType int NULL,
	IsDefault bit NULL,
	LinkID nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDCategory nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	Modified bit NULL,
	Name nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	SettingsType int NULL,
	WordWrap bit NULL,
	ID uniqueidentifier DEFAULT newsequentialid() NOT NULL,
	SortAscending bit NULL,
	SortColumn int NULL,
	ColumnOrder nvarchar(MAX) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	ExportGroupRows bit NULL,
	Filters nvarchar(MAX) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	FrozenColumns int NULL,
	Groups nvarchar(MAX) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	HiddenColumns nvarchar(MAX) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	ColumnSortFlags nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	ColumnSortPrecedence nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	MasterColumnOrder varchar(MAX) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	MaxSortColumns int NULL,
	ShowNumColumn bit NULL,
	CONSTRAINT PK_SavedViews PRIMARY KEY (ID)
);
 CREATE NONCLUSTERED INDEX idxLinkID ON MicrovellumData.dbo.SavedViews (  LinkID ASC  )  
	 WITH (  PAD_INDEX = OFF ,FILLFACTOR = 100  ,SORT_IN_TEMPDB = OFF , IGNORE_DUP_KEY = OFF , STATISTICS_NORECOMPUTE = OFF , ONLINE = OFF , ALLOW_ROW_LOCKS = ON , ALLOW_PAGE_LOCKS = ON  )
	 ON [PRIMARY ] ;


-- MicrovellumData.dbo.SawStacks definition

-- Drop table

-- DROP TABLE MicrovellumData.dbo.SawStacks;

CREATE TABLE MicrovellumData.dbo.SawStacks (
	LinkID nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDCategory nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkidProcessingStation nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDWorkOrder nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDWorkOrderBatch nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	MaterialName nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	Modified bit NULL,
	Name nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	StackHeight int NULL,
	ID uniqueidentifier DEFAULT newsequentialid() NOT NULL,
	CONSTRAINT PK_SawStacks PRIMARY KEY (ID)
);
 CREATE NONCLUSTERED INDEX idxLinkID ON MicrovellumData.dbo.SawStacks (  LinkID ASC  )  
	 WITH (  PAD_INDEX = OFF ,FILLFACTOR = 100  ,SORT_IN_TEMPDB = OFF , IGNORE_DUP_KEY = OFF , STATISTICS_NORECOMPUTE = OFF , ONLINE = OFF , ALLOW_ROW_LOCKS = ON , ALLOW_PAGE_LOCKS = ON  )
	 ON [PRIMARY ] ;


-- MicrovellumData.dbo.SawingOptimizerSettings definition

-- Drop table

-- DROP TABLE MicrovellumData.dbo.SawingOptimizerSettings;

CREATE TABLE MicrovellumData.dbo.SawingOptimizerSettings (
	ConnectionName nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	CPOUTAlgorithm int NULL,
	CPOUTCode int NULL,
	CreateSawOFFRecords bit NULL,
	DecimalPlaces int NULL,
	EdgebandPremill float NULL,
	GrippersWidth float NULL,
	LinkID nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDCategory nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	MaxPartRecords int NULL,
	Modified bit NULL,
	Name nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	NumberOfPhases int NULL,
	PerfectGrainTrim float NULL,
	ReCutTrailingTrim float NULL,
	RoundingType int NULL,
	SawAxisType int NULL,
	SawBladeWidth float NULL,
	SawCutPreference int NULL,
	SawLinkType int NULL,
	SawUnitsType int NULL,
	SheetOrigin int NULL,
	ShowPartIndexNumber bit NULL,
	SimpleCPOUT bit NULL,
	StackHeight float NULL,
	StackQty int NULL,
	YieldStrategyLevel int NULL,
	ID uniqueidentifier DEFAULT newsequentialid() NOT NULL,
	OversizeAmount float NULL,
	PrefectGrainType int NULL,
	AuxPath1 nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	UseAlphanumericExtensions bit NULL,
	DestinationPath nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	MaxFileNumberRange int NULL,
	MinFileNumberRange int NULL,
	SheetOriginRelative bit NULL,
	OptimizerTimeLimitIndex int NULL,
	ExcludeOversizeInPartsReq bit NULL,
	CutPreference int NULL,
	OptimizerTimeAllowance int NULL,
	StackedMaterialPreference int NULL,
	AllowMixedStacking bit NULL,
	OptimizerPersistence int NULL,
	SheetSelectionStrategy int NULL,
	UseHandlingCode bit NULL,
	CONSTRAINT PK_SawingOptimizerSettings PRIMARY KEY (ID)
);
 CREATE NONCLUSTERED INDEX idxLinkID ON MicrovellumData.dbo.SawingOptimizerSettings (  LinkID ASC  )  
	 WITH (  PAD_INDEX = OFF ,FILLFACTOR = 100  ,SORT_IN_TEMPDB = OFF , IGNORE_DUP_KEY = OFF , STATISTICS_NORECOMPUTE = OFF , ONLINE = OFF , ALLOW_ROW_LOCKS = ON , ALLOW_PAGE_LOCKS = ON  )
	 ON [PRIMARY ] ;


-- MicrovellumData.dbo.ScheduledWorkOrderActivities definition

-- Drop table

-- DROP TABLE MicrovellumData.dbo.ScheduledWorkOrderActivities;

CREATE TABLE MicrovellumData.dbo.ScheduledWorkOrderActivities (
	[Date] datetime NULL,
	EndTime datetime NULL,
	LinkID nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDActivityStation nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDCategory nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDPredecessor nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDShift nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDWorkOrder nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDWorkOrderActivity nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	Modified bit NULL,
	Name nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	PredecessorLagDays float NULL,
	PredecessorType int NULL,
	ScheduledValue float NULL,
	StartTime datetime NULL,
	[Type] int NULL,
	ID uniqueidentifier DEFAULT newsequentialid() NOT NULL,
	GroupingKey nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	Priority float NULL,
	CONSTRAINT PK_ScheduledWorkOrderActivities PRIMARY KEY (ID)
);
 CREATE NONCLUSTERED INDEX idxLinkID ON MicrovellumData.dbo.ScheduledWorkOrderActivities (  LinkID ASC  )  
	 WITH (  PAD_INDEX = OFF ,FILLFACTOR = 100  ,SORT_IN_TEMPDB = OFF , IGNORE_DUP_KEY = OFF , STATISTICS_NORECOMPUTE = OFF , ONLINE = OFF , ALLOW_ROW_LOCKS = ON , ALLOW_PAGE_LOCKS = ON  )
	 ON [PRIMARY ] ;


-- MicrovellumData.dbo.ScrapOptimizerSettings definition

-- Drop table

-- DROP TABLE MicrovellumData.dbo.ScrapOptimizerSettings;

CREATE TABLE MicrovellumData.dbo.ScrapOptimizerSettings (
	Destroy bit NULL,
	DestroyMaxDim1 float NULL,
	DestroyMaxDim2 float NULL,
	DestroySequence int NULL,
	DestroyTool nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	DestroyType int NULL,
	DimAndLogic bit NULL,
	LinkID nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDCategory nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	MinArea float NULL,
	MinDim float NULL,
	Modified bit NULL,
	Name nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	OverrideTrims bit NULL,
	[Save] bit NULL,
	TrimLeadingLength float NULL,
	TrimLeadingWidth float NULL,
	TrimTrailingLength float NULL,
	TrimTrailingWidth float NULL,
	[Use] bit NULL,
	ID uniqueidentifier DEFAULT newsequentialid() NOT NULL,
	PromptToCommit bit NULL,
	SaveType int NULL,
	AutoCommitUsage bit NULL,
	DestroyReduceToolDrops bit NULL,
	DestroySheetTrim float NULL,
	ScrapUsagePreference int NULL,
	CONSTRAINT PK_ScrapOptimizerSettings PRIMARY KEY (ID)
);
 CREATE NONCLUSTERED INDEX idxLinkID ON MicrovellumData.dbo.ScrapOptimizerSettings (  LinkID ASC  )  
	 WITH (  PAD_INDEX = OFF ,FILLFACTOR = 100  ,SORT_IN_TEMPDB = OFF , IGNORE_DUP_KEY = OFF , STATISTICS_NORECOMPUTE = OFF , ONLINE = OFF , ALLOW_ROW_LOCKS = ON , ALLOW_PAGE_LOCKS = ON  )
	 ON [PRIMARY ] ;


-- MicrovellumData.dbo.ScrapOptimizerSizes definition

-- Drop table

-- DROP TABLE MicrovellumData.dbo.ScrapOptimizerSizes;

CREATE TABLE MicrovellumData.dbo.ScrapOptimizerSizes (
	LinkID nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDCategory nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDParent nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	MaxX float NULL,
	MaxY float NULL,
	MinX float NULL,
	MinY float NULL,
	Modified bit NULL,
	Name nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	Priority int NULL,
	ID uniqueidentifier DEFAULT newsequentialid() NOT NULL,
	CONSTRAINT PK_ScrapOptimizerSizes PRIMARY KEY (ID)
);
 CREATE NONCLUSTERED INDEX idxLinkID ON MicrovellumData.dbo.ScrapOptimizerSizes (  LinkID ASC  )  
	 WITH (  PAD_INDEX = OFF ,FILLFACTOR = 100  ,SORT_IN_TEMPDB = OFF , IGNORE_DUP_KEY = OFF , STATISTICS_NORECOMPUTE = OFF , ONLINE = OFF , ALLOW_ROW_LOCKS = ON , ALLOW_PAGE_LOCKS = ON  )
	 ON [PRIMARY ] ;


-- MicrovellumData.dbo.SheetSets definition

-- Drop table

-- DROP TABLE MicrovellumData.dbo.SheetSets;

CREATE TABLE MicrovellumData.dbo.SheetSets (
	LinkID nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDCategory nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDProject nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	ModelSpaceFileName nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	Modified bit NULL,
	Name nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	PaperSpaceFileName nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	ID uniqueidentifier DEFAULT newsequentialid() NOT NULL,
	CONSTRAINT PK_SheetSets PRIMARY KEY (ID)
);
 CREATE NONCLUSTERED INDEX idxLinkID ON MicrovellumData.dbo.SheetSets (  LinkID ASC  )  
	 WITH (  PAD_INDEX = OFF ,FILLFACTOR = 100  ,SORT_IN_TEMPDB = OFF , IGNORE_DUP_KEY = OFF , STATISTICS_NORECOMPUTE = OFF , ONLINE = OFF , ALLOW_ROW_LOCKS = ON , ALLOW_PAGE_LOCKS = ON  )
	 ON [PRIMARY ] ;


-- MicrovellumData.dbo.Sheets definition

-- Drop table

-- DROP TABLE MicrovellumData.dbo.Sheets;

CREATE TABLE MicrovellumData.dbo.Sheets (
	Code nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	Comments nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	DatabaseTableNameType int NULL,
	Grain int NULL,
	HatchType int NULL,
	[Index] int NULL,
	InventoryAvailableQty float NULL,
	InventoryCurrentQty float NULL,
	InventoryMinQty float NULL,
	LeadingLengthTrim float NULL,
	LeadingWidthTrim float NULL,
	[Length] float NULL,
	LinkID nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDBottomFaceRendering nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDCategory nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDCoreRendering nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDDefaultVendor nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDMaterial nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDSheetSize nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDStack nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDSubType nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDTopFaceRendering nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	MarkUp float NULL,
	MaterialCost float NULL,
	MaterialEstimateCost float NULL,
	Modified bit NULL,
	Name nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	NumplacedParts int NULL,
	NumStrips int NULL,
	OptimizationPriority int NULL,
	Pattern int NULL,
	Picture varbinary(MAX) NULL,
	PrintFlag bit NULL,
	Quantity float NULL,
	Scrap nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	Thickness float NULL,
	TrailingLengthTrim float NULL,
	TrailingWidthTrim float NULL,
	[Type] int NULL,
	UnitType int NULL,
	UsableLength float NULL,
	UsableWidth float NULL,
	WasteFactor float NULL,
	Width float NULL,
	ID uniqueidentifier DEFAULT newsequentialid() NOT NULL,
	LinkIDMaterialStorageLocation nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDProcessingStation nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDWorkOrderBatch nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	ScrapType int NULL,
	LinkIDWorkOrder nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	HandlingCode nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	CodeFormula varchar(MAX) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	GrainFormula varchar(MAX) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	HandlingCodeFormula varchar(MAX) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	IsFormulaMaterial bit NULL,
	LeadingLengthTrimFormula varchar(MAX) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LeadingWidthTrimFormula varchar(MAX) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LengthFormula varchar(MAX) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	MarkUpFormula varchar(MAX) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	MaterialCommentsFormula varchar(MAX) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	MaterialCostFormula varchar(MAX) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	OptimizationPriorityFormula varchar(MAX) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	QtyFormula varchar(MAX) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	TrailingLengthTrimFormula varchar(MAX) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	TrailingWidthTrimFormula varchar(MAX) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	WasteFactorFormula varchar(MAX) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	WidthFormula varchar(MAX) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	MaterialFlipSetting int NULL,
	SpredsheetColumn int NULL,
	MaterialLaborValue float NULL,
	MaterialLaborValueFormula varchar(MAX) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	MaterialXData1 varchar(MAX) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	MaterialXData1Formula varchar(MAX) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	MaterialXData2 varchar(MAX) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	MaterialXData2Formula varchar(MAX) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	MaterialXData3 varchar(MAX) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	MaterialXData3Formula varchar(MAX) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	Region int NULL,
	SkipSpreadsheetSync bit NULL,
	WeightedSelectionValue int NULL,
	WeightedSelectionValueFormula varchar(MAX) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	CONSTRAINT PK_Sheets PRIMARY KEY (ID)
);
 CREATE NONCLUSTERED INDEX idxLinkID ON MicrovellumData.dbo.Sheets (  LinkID ASC  )  
	 WITH (  PAD_INDEX = OFF ,FILLFACTOR = 100  ,SORT_IN_TEMPDB = OFF , IGNORE_DUP_KEY = OFF , STATISTICS_NORECOMPUTE = OFF , ONLINE = OFF , ALLOW_ROW_LOCKS = ON , ALLOW_PAGE_LOCKS = ON  )
	 ON [PRIMARY ] ;


-- MicrovellumData.dbo.SheetsVendors definition

-- Drop table

-- DROP TABLE MicrovellumData.dbo.SheetsVendors;

CREATE TABLE MicrovellumData.dbo.SheetsVendors (
	LinkIDSheet nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDVendor nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	ID uniqueidentifier DEFAULT newsequentialid() NOT NULL,
	CONSTRAINT PK_SheetsVendors PRIMARY KEY (ID)
);
 CREATE NONCLUSTERED INDEX idxLinkIDSheet ON MicrovellumData.dbo.SheetsVendors (  LinkIDSheet ASC  )  
	 WITH (  PAD_INDEX = OFF ,FILLFACTOR = 100  ,SORT_IN_TEMPDB = OFF , IGNORE_DUP_KEY = OFF , STATISTICS_NORECOMPUTE = OFF , ONLINE = OFF , ALLOW_ROW_LOCKS = ON , ALLOW_PAGE_LOCKS = ON  )
	 ON [PRIMARY ] ;
 CREATE NONCLUSTERED INDEX idxLinkIDVendor ON MicrovellumData.dbo.SheetsVendors (  LinkIDVendor ASC  )  
	 WITH (  PAD_INDEX = OFF ,FILLFACTOR = 100  ,SORT_IN_TEMPDB = OFF , IGNORE_DUP_KEY = OFF , STATISTICS_NORECOMPUTE = OFF , ONLINE = OFF , ALLOW_ROW_LOCKS = ON , ALLOW_PAGE_LOCKS = ON  )
	 ON [PRIMARY ] ;


-- MicrovellumData.dbo.Shifts definition

-- Drop table

-- DROP TABLE MicrovellumData.dbo.Shifts;

CREATE TABLE MicrovellumData.dbo.Shifts (
	AllowFriday bit NULL,
	AllowMonday bit NULL,
	AllowSaturday bit NULL,
	AllowSunday bit NULL,
	AllowThursday bit NULL,
	AllowTuesday bit NULL,
	AllowWednesday bit NULL,
	ClockInBuffer int NULL,
	ClockInInterval int NULL,
	ClockOutBuffer int NULL,
	ClockOutInterval int NULL,
	EndTime datetime NULL,
	LinkID nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDCategory nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	Modified bit NULL,
	Name nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	OvertimeThresholdDay float NULL,
	OvertimeThresholdWeek float NULL,
	PayMultiplier float NULL,
	ShiftDurationTime float NULL,
	ShiftProductionTime float NULL,
	StartTime datetime NULL,
	[Type] int NULL,
	UnPaidBreakEndTime datetime NULL,
	UnPaidBreakStartTime datetime NULL,
	ID uniqueidentifier DEFAULT newsequentialid() NOT NULL,
	CONSTRAINT PK_Shifts PRIMARY KEY (ID)
);
 CREATE NONCLUSTERED INDEX idxLinkID ON MicrovellumData.dbo.Shifts (  LinkID ASC  )  
	 WITH (  PAD_INDEX = OFF ,FILLFACTOR = 100  ,SORT_IN_TEMPDB = OFF , IGNORE_DUP_KEY = OFF , STATISTICS_NORECOMPUTE = OFF , ONLINE = OFF , ALLOW_ROW_LOCKS = ON , ALLOW_PAGE_LOCKS = ON  )
	 ON [PRIMARY ] ;


-- MicrovellumData.dbo.ShiftsActivityStations definition

-- Drop table

-- DROP TABLE MicrovellumData.dbo.ShiftsActivityStations;

CREATE TABLE MicrovellumData.dbo.ShiftsActivityStations (
	LinkIDActivityStation nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDShift nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	ID uniqueidentifier DEFAULT newsequentialid() NOT NULL,
	CONSTRAINT PK_ShiftsActivityStations PRIMARY KEY (ID)
);
 CREATE NONCLUSTERED INDEX idxLinkIDActivityStation ON MicrovellumData.dbo.ShiftsActivityStations (  LinkIDActivityStation ASC  )  
	 WITH (  PAD_INDEX = OFF ,FILLFACTOR = 100  ,SORT_IN_TEMPDB = OFF , IGNORE_DUP_KEY = OFF , STATISTICS_NORECOMPUTE = OFF , ONLINE = OFF , ALLOW_ROW_LOCKS = ON , ALLOW_PAGE_LOCKS = ON  )
	 ON [PRIMARY ] ;
 CREATE NONCLUSTERED INDEX idxLinkIDShift ON MicrovellumData.dbo.ShiftsActivityStations (  LinkIDShift ASC  )  
	 WITH (  PAD_INDEX = OFF ,FILLFACTOR = 100  ,SORT_IN_TEMPDB = OFF , IGNORE_DUP_KEY = OFF , STATISTICS_NORECOMPUTE = OFF , ONLINE = OFF , ALLOW_ROW_LOCKS = ON , ALLOW_PAGE_LOCKS = ON  )
	 ON [PRIMARY ] ;


-- MicrovellumData.dbo.ShiftsBreaks definition

-- Drop table

-- DROP TABLE MicrovellumData.dbo.ShiftsBreaks;

CREATE TABLE MicrovellumData.dbo.ShiftsBreaks (
	LinkIDBreak nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDShift nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	ID uniqueidentifier DEFAULT newsequentialid() NOT NULL,
	CONSTRAINT PK_ShiftsBreaks PRIMARY KEY (ID)
);
 CREATE NONCLUSTERED INDEX idxLinkIDBreak ON MicrovellumData.dbo.ShiftsBreaks (  LinkIDBreak ASC  )  
	 WITH (  PAD_INDEX = OFF ,FILLFACTOR = 100  ,SORT_IN_TEMPDB = OFF , IGNORE_DUP_KEY = OFF , STATISTICS_NORECOMPUTE = OFF , ONLINE = OFF , ALLOW_ROW_LOCKS = ON , ALLOW_PAGE_LOCKS = ON  )
	 ON [PRIMARY ] ;
 CREATE NONCLUSTERED INDEX idxLinkIDShift ON MicrovellumData.dbo.ShiftsBreaks (  LinkIDShift ASC  )  
	 WITH (  PAD_INDEX = OFF ,FILLFACTOR = 100  ,SORT_IN_TEMPDB = OFF , IGNORE_DUP_KEY = OFF , STATISTICS_NORECOMPUTE = OFF , ONLINE = OFF , ALLOW_ROW_LOCKS = ON , ALLOW_PAGE_LOCKS = ON  )
	 ON [PRIMARY ] ;


-- MicrovellumData.dbo.ShiftsHolidays definition

-- Drop table

-- DROP TABLE MicrovellumData.dbo.ShiftsHolidays;

CREATE TABLE MicrovellumData.dbo.ShiftsHolidays (
	LinkIDHoliday nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDShift nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	ID uniqueidentifier DEFAULT newsequentialid() NOT NULL,
	CONSTRAINT PK_ShiftsHolidays PRIMARY KEY (ID)
);
 CREATE NONCLUSTERED INDEX idxLinkIDHoliday ON MicrovellumData.dbo.ShiftsHolidays (  LinkIDHoliday ASC  )  
	 WITH (  PAD_INDEX = OFF ,FILLFACTOR = 100  ,SORT_IN_TEMPDB = OFF , IGNORE_DUP_KEY = OFF , STATISTICS_NORECOMPUTE = OFF , ONLINE = OFF , ALLOW_ROW_LOCKS = ON , ALLOW_PAGE_LOCKS = ON  )
	 ON [PRIMARY ] ;
 CREATE NONCLUSTERED INDEX idxLinkIDShift ON MicrovellumData.dbo.ShiftsHolidays (  LinkIDShift ASC  )  
	 WITH (  PAD_INDEX = OFF ,FILLFACTOR = 100  ,SORT_IN_TEMPDB = OFF , IGNORE_DUP_KEY = OFF , STATISTICS_NORECOMPUTE = OFF , ONLINE = OFF , ALLOW_ROW_LOCKS = ON , ALLOW_PAGE_LOCKS = ON  )
	 ON [PRIMARY ] ;


-- MicrovellumData.dbo.ShippingItems definition

-- Drop table

-- DROP TABLE MicrovellumData.dbo.ShippingItems;

CREATE TABLE MicrovellumData.dbo.ShippingItems (
	Comments nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	DateCreated datetime NULL,
	DateDelivered datetime NULL,
	DateLoaded datetime NULL,
	DateShipped datetime NULL,
	[Depth] float NULL,
	Description nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	Height float NULL,
	LinkID nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDCategory nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDEmployeeCreator nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDEmployeeDeliver nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDEmployeeLoader nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDHardwarePart nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDMaterial nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDPallet nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDParentProduct nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDPart nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDProject nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDShippingTicket nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDSubassembly nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDWorkOrder nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	Modified bit NULL,
	Name nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	PieceCount float NULL,
	Quantity float NULL,
	QuantityShipped float NULL,
	RoomName nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	Status int NULL,
	[Type] int NULL,
	Weight float NULL,
	Width float NULL,
	ID uniqueidentifier DEFAULT newsequentialid() NOT NULL,
	LinkIDParentItem nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	CONSTRAINT PK_ShippingItems PRIMARY KEY (ID)
);
 CREATE NONCLUSTERED INDEX idxLinkID ON MicrovellumData.dbo.ShippingItems (  LinkID ASC  )  
	 WITH (  PAD_INDEX = OFF ,FILLFACTOR = 100  ,SORT_IN_TEMPDB = OFF , IGNORE_DUP_KEY = OFF , STATISTICS_NORECOMPUTE = OFF , ONLINE = OFF , ALLOW_ROW_LOCKS = ON , ALLOW_PAGE_LOCKS = ON  )
	 ON [PRIMARY ] ;


-- MicrovellumData.dbo.ShippingTickets definition

-- Drop table

-- DROP TABLE MicrovellumData.dbo.ShippingTickets;

CREATE TABLE MicrovellumData.dbo.ShippingTickets (
	Comments nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	DateActualDelivery datetime NULL,
	DateCreated datetime NULL,
	DateDeliveryScheduled datetime NULL,
	LinkID nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDCategory nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDContactCompany nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDEmployeeCreator nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	Modified bit NULL,
	Name nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	ShipMethod int NULL,
	ShippingTime int NULL,
	ShipStatus int NULL,
	ShipToAddress nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	TicketNumber int NULL,
	ID uniqueidentifier DEFAULT newsequentialid() NOT NULL,
	CONSTRAINT PK_ShippingTickets PRIMARY KEY (ID)
);
 CREATE NONCLUSTERED INDEX idxLinkID ON MicrovellumData.dbo.ShippingTickets (  LinkID ASC  )  
	 WITH (  PAD_INDEX = OFF ,FILLFACTOR = 100  ,SORT_IN_TEMPDB = OFF , IGNORE_DUP_KEY = OFF , STATISTICS_NORECOMPUTE = OFF , ONLINE = OFF , ALLOW_ROW_LOCKS = ON , ALLOW_PAGE_LOCKS = ON  )
	 ON [PRIMARY ] ;


-- MicrovellumData.dbo.SpecificationGroups definition

-- Drop table

-- DROP TABLE MicrovellumData.dbo.SpecificationGroups;

CREATE TABLE MicrovellumData.dbo.SpecificationGroups (
	CategoryLevel int NULL,
	LinkID nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDCategory nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDCutPartsFileName nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDDoorWizardFileName nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDEdgeBandFileName nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDFactoryFileName nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDGlobalFileName nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDHardwareFileName nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDLibrary nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDProject nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDProjectWizardFileName nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	Modified bit NULL,
	Name nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	PrintFlag bit NULL,
	[Type] int NULL,
	ID uniqueidentifier DEFAULT newsequentialid() NOT NULL,
	CONSTRAINT PK_SpecificationGroups PRIMARY KEY (ID)
);
 CREATE NONCLUSTERED INDEX idxLinkID ON MicrovellumData.dbo.SpecificationGroups (  LinkID ASC  )  
	 WITH (  PAD_INDEX = OFF ,FILLFACTOR = 100  ,SORT_IN_TEMPDB = OFF , IGNORE_DUP_KEY = OFF , STATISTICS_NORECOMPUTE = OFF , ONLINE = OFF , ALLOW_ROW_LOCKS = ON , ALLOW_PAGE_LOCKS = ON  )
	 ON [PRIMARY ] ;


-- MicrovellumData.dbo.StorageGroupItems definition

-- Drop table

-- DROP TABLE MicrovellumData.dbo.StorageGroupItems;

CREATE TABLE MicrovellumData.dbo.StorageGroupItems (
	LinkID nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDItem nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDStorageGroup nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	Name nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	ScanCode nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[Type] int NULL,
	ID uniqueidentifier DEFAULT newsequentialid() NOT NULL,
	CONSTRAINT PK_StorageGroupItems PRIMARY KEY (ID)
);
 CREATE NONCLUSTERED INDEX idxLinkID ON MicrovellumData.dbo.StorageGroupItems (  LinkID ASC  )  
	 WITH (  PAD_INDEX = OFF ,FILLFACTOR = 100  ,SORT_IN_TEMPDB = OFF , IGNORE_DUP_KEY = OFF , STATISTICS_NORECOMPUTE = OFF , ONLINE = OFF , ALLOW_ROW_LOCKS = ON , ALLOW_PAGE_LOCKS = ON  )
	 ON [PRIMARY ] ;


-- MicrovellumData.dbo.StorageGroups definition

-- Drop table

-- DROP TABLE MicrovellumData.dbo.StorageGroups;

CREATE TABLE MicrovellumData.dbo.StorageGroups (
	Comments nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	DateCreated datetime NULL,
	LinkID nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDEmployee nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDStorageLocation nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	Name nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	Number nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	PrintFlag bit NULL,
	ScanCode nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	Status int NULL,
	[Type] int NULL,
	ID uniqueidentifier DEFAULT newsequentialid() NOT NULL,
	CONSTRAINT PK_StorageGroups PRIMARY KEY (ID)
);
 CREATE NONCLUSTERED INDEX idxLinkID ON MicrovellumData.dbo.StorageGroups (  LinkID ASC  )  
	 WITH (  PAD_INDEX = OFF ,FILLFACTOR = 100  ,SORT_IN_TEMPDB = OFF , IGNORE_DUP_KEY = OFF , STATISTICS_NORECOMPUTE = OFF , ONLINE = OFF , ALLOW_ROW_LOCKS = ON , ALLOW_PAGE_LOCKS = ON  )
	 ON [PRIMARY ] ;


-- MicrovellumData.dbo.Subassemblies definition

-- Drop table

-- DROP TABLE MicrovellumData.dbo.Subassemblies;

CREATE TABLE MicrovellumData.dbo.Subassemblies (
	ActivityPath nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	ActivityPathShort nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	Angle float NULL,
	BayPosition float NULL,
	Comments nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	Comments1 nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	Comments2 nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	Comments3 nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	ContainsSubassemblies bit NULL,
	[Depth] float NULL,
	DrawIndex int NULL,
	Height float NULL,
	IsBuyOut bit NULL,
	JPegName nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	JPegStream varbinary(MAX) NULL,
	LinkID nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDCategory nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDLibrary nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDParentProduct nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDParentSubassembly nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDProject nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	Modified bit NULL,
	ModifiedRowID int NULL,
	Name nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	PerfectGrainCaption nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	Quantity int NULL,
	TiffName nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	TiffStream varbinary(MAX) NULL,
	[Type] int NULL,
	Width float NULL,
	WMFName nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	WMFStream varbinary(MAX) NULL,
	WorkBook varbinary(MAX) NULL,
	X float NULL,
	Y float NULL,
	Z float NULL,
	ID uniqueidentifier DEFAULT newsequentialid() NOT NULL,
	LinkIDWorkOrder nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	ScanCode nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	CONSTRAINT PK_Subassemblies PRIMARY KEY (ID)
);
 CREATE NONCLUSTERED INDEX idxLinkID ON MicrovellumData.dbo.Subassemblies (  LinkID ASC  )  
	 WITH (  PAD_INDEX = OFF ,FILLFACTOR = 100  ,SORT_IN_TEMPDB = OFF , IGNORE_DUP_KEY = OFF , STATISTICS_NORECOMPUTE = OFF , ONLINE = OFF , ALLOW_ROW_LOCKS = ON , ALLOW_PAGE_LOCKS = ON  )
	 ON [PRIMARY ] ;
 CREATE NONCLUSTERED INDEX linkidparent_idx ON MicrovellumData.dbo.Subassemblies (  LinkIDParentProduct ASC  )  
	 WITH (  PAD_INDEX = OFF ,FILLFACTOR = 100  ,SORT_IN_TEMPDB = OFF , IGNORE_DUP_KEY = OFF , STATISTICS_NORECOMPUTE = OFF , ONLINE = OFF , ALLOW_ROW_LOCKS = ON , ALLOW_PAGE_LOCKS = ON  )
	 ON [PRIMARY ] ;


-- MicrovellumData.dbo.TiffDrawings definition

-- Drop table

-- DROP TABLE MicrovellumData.dbo.TiffDrawings;

CREATE TABLE MicrovellumData.dbo.TiffDrawings (
	AdvancedOverlay varbinary(MAX) NULL,
	AnnotationStream varbinary(MAX) NULL,
	CustomScale float NULL,
	DrawingUnits int NULL,
	LinkID nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDCategory nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDProject nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	Modified bit NULL,
	Name nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	RevisionNumber nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	Rotation int NULL,
	[Scale] int NULL,
	StandardOverlay varbinary(MAX) NULL,
	TiffStream varbinary(MAX) NULL,
	ID uniqueidentifier DEFAULT newsequentialid() NOT NULL,
	CONSTRAINT PK_TiffDrawings PRIMARY KEY (ID)
);
 CREATE NONCLUSTERED INDEX idxLinkID ON MicrovellumData.dbo.TiffDrawings (  LinkID ASC  )  
	 WITH (  PAD_INDEX = OFF ,FILLFACTOR = 100  ,SORT_IN_TEMPDB = OFF , IGNORE_DUP_KEY = OFF , STATISTICS_NORECOMPUTE = OFF , ONLINE = OFF , ALLOW_ROW_LOCKS = ON , ALLOW_PAGE_LOCKS = ON  )
	 ON [PRIMARY ] ;


-- MicrovellumData.dbo.TimeClockSettings definition

-- Drop table

-- DROP TABLE MicrovellumData.dbo.TimeClockSettings;

CREATE TABLE MicrovellumData.dbo.TimeClockSettings (
	GeoFenceMaxDistance float NULL,
	LinkID nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDCategory nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	MainImbedUrl nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	Modified bit NULL,
	Name nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	TimeClockSettings int NULL,
	ID uniqueidentifier DEFAULT newsequentialid() NOT NULL,
	CONSTRAINT PK_TimeClockSettings PRIMARY KEY (ID)
);
 CREATE NONCLUSTERED INDEX idxLinkID ON MicrovellumData.dbo.TimeClockSettings (  LinkID ASC  )  
	 WITH (  PAD_INDEX = OFF ,FILLFACTOR = 100  ,SORT_IN_TEMPDB = OFF , IGNORE_DUP_KEY = OFF , STATISTICS_NORECOMPUTE = OFF , ONLINE = OFF , ALLOW_ROW_LOCKS = ON , ALLOW_PAGE_LOCKS = ON  )
	 ON [PRIMARY ] ;


-- MicrovellumData.dbo.ToolFiles definition

-- Drop table

-- DROP TABLE MicrovellumData.dbo.ToolFiles;

CREATE TABLE MicrovellumData.dbo.ToolFiles (
	LinkID nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDCategory nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDFactory nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	Modified bit NULL,
	Name nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	WorkBook varbinary(MAX) NULL,
	ID uniqueidentifier DEFAULT newsequentialid() NOT NULL,
	CONSTRAINT PK_ToolFiles PRIMARY KEY (ID)
);
 CREATE NONCLUSTERED INDEX idxLinkID ON MicrovellumData.dbo.ToolFiles (  LinkID ASC  )  
	 WITH (  PAD_INDEX = OFF ,FILLFACTOR = 100  ,SORT_IN_TEMPDB = OFF , IGNORE_DUP_KEY = OFF , STATISTICS_NORECOMPUTE = OFF , ONLINE = OFF , ALLOW_ROW_LOCKS = ON , ALLOW_PAGE_LOCKS = ON  )
	 ON [PRIMARY ] ;


-- MicrovellumData.dbo.UserDefinedTokens definition

-- Drop table

-- DROP TABLE MicrovellumData.dbo.UserDefinedTokens;

CREATE TABLE MicrovellumData.dbo.UserDefinedTokens (
	LinkID nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDCategory nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	Modified bit NULL,
	Name nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[Type] int NULL,
	UserDefinedByteArray varbinary(MAX) NULL,
	ID uniqueidentifier DEFAULT newsequentialid() NOT NULL,
	CONSTRAINT PK_UserDefinedTokens PRIMARY KEY (ID)
);
 CREATE NONCLUSTERED INDEX idxLinkID ON MicrovellumData.dbo.UserDefinedTokens (  LinkID ASC  )  
	 WITH (  PAD_INDEX = OFF ,FILLFACTOR = 100  ,SORT_IN_TEMPDB = OFF , IGNORE_DUP_KEY = OFF , STATISTICS_NORECOMPUTE = OFF , ONLINE = OFF , ALLOW_ROW_LOCKS = ON , ALLOW_PAGE_LOCKS = ON  )
	 ON [PRIMARY ] ;


-- MicrovellumData.dbo.Vendors definition

-- Drop table

-- DROP TABLE MicrovellumData.dbo.Vendors;

CREATE TABLE MicrovellumData.dbo.Vendors (
	AccountNumber nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	CellPhone nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	City nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	Comments nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	Country nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	EmailDefault nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	FaxDefault nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkID nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDCategory nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDPrimaryContact nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDReportTemplate nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	MailCity nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	MailCountry nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	MailPOBox nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	MailState nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	MailStreet nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	MailZipCode nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	Modified bit NULL,
	Name nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	PaymentTermsType int NULL,
	PhoneDefault nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	POBox nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	State nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	Street nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	WebSiteDefault nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	ZipCode nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	ID uniqueidentifier DEFAULT newsequentialid() NOT NULL,
	[Type] int NULL,
	CONSTRAINT PK_Vendors PRIMARY KEY (ID)
);
 CREATE NONCLUSTERED INDEX idxLinkID ON MicrovellumData.dbo.Vendors (  LinkID ASC  )  
	 WITH (  PAD_INDEX = OFF ,FILLFACTOR = 100  ,SORT_IN_TEMPDB = OFF , IGNORE_DUP_KEY = OFF , STATISTICS_NORECOMPUTE = OFF , ONLINE = OFF , ALLOW_ROW_LOCKS = ON , ALLOW_PAGE_LOCKS = ON  )
	 ON [PRIMARY ] ;


-- MicrovellumData.dbo.WorkOrderActivities definition

-- Drop table

-- DROP TABLE MicrovellumData.dbo.WorkOrderActivities;

CREATE TABLE MicrovellumData.dbo.WorkOrderActivities (
	ActualValue float NULL,
	Comments nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	DateStatusChanged datetime NULL,
	EndDate datetime NULL,
	LaborRate float NULL,
	LinkID nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDActivityStation nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDCategory nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDPart nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDProject nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDShift nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDStatusChangeEmployee nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDSubassembly nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDWorkOrder nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkProduct nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	Modified bit NULL,
	Name nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	NumberOfEmployeesRequired int NULL,
	ScheduledValue float NULL,
	SequenceNumber int NULL,
	StartDate datetime NULL,
	StationNumber float NULL,
	Status int NULL,
	TotalHours float NULL,
	TotalScheduledValue float NULL,
	[Type] int NULL,
	UnitsPerMinuteFactoryTime float NULL,
	UnitType int NULL,
	ID uniqueidentifier DEFAULT newsequentialid() NOT NULL,
	GroupingKey nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDScheduledActivity nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	Priority float NULL,
	CONSTRAINT PK_WorkOrderActivities PRIMARY KEY (ID)
);
 CREATE NONCLUSTERED INDEX idxLinkID ON MicrovellumData.dbo.WorkOrderActivities (  LinkID ASC  )  
	 WITH (  PAD_INDEX = OFF ,FILLFACTOR = 100  ,SORT_IN_TEMPDB = OFF , IGNORE_DUP_KEY = OFF , STATISTICS_NORECOMPUTE = OFF , ONLINE = OFF , ALLOW_ROW_LOCKS = ON , ALLOW_PAGE_LOCKS = ON  )
	 ON [PRIMARY ] ;


-- MicrovellumData.dbo.WorkOrderBatches definition

-- Drop table

-- DROP TABLE MicrovellumData.dbo.WorkOrderBatches;

CREATE TABLE MicrovellumData.dbo.WorkOrderBatches (
	LinkID nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDWorkOrder nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	Name nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	ID uniqueidentifier DEFAULT newsequentialid() NOT NULL,
	CONSTRAINT PK_WorkOrderBatches PRIMARY KEY (ID)
);
 CREATE NONCLUSTERED INDEX idxLinkID ON MicrovellumData.dbo.WorkOrderBatches (  LinkID ASC  )  
	 WITH (  PAD_INDEX = OFF ,FILLFACTOR = 100  ,SORT_IN_TEMPDB = OFF , IGNORE_DUP_KEY = OFF , STATISTICS_NORECOMPUTE = OFF , ONLINE = OFF , ALLOW_ROW_LOCKS = ON , ALLOW_PAGE_LOCKS = ON  )
	 ON [PRIMARY ] ;


-- MicrovellumData.dbo.Workorders definition

-- Drop table

-- DROP TABLE MicrovellumData.dbo.Workorders;

CREATE TABLE MicrovellumData.dbo.Workorders (
	Active bit NULL,
	ActualCompletionDate datetime NULL,
	Comments nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	Estimate bit NULL,
	LinkID nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	LinkIDCategory nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	Modified bit NULL,
	Name nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	Number nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	ScheduledCompletionDate datetime NULL,
	ScheduledStartDate datetime NULL,
	ShippingTime int NULL,
	Status int NULL,
	WorkOrderNumber int NULL,
	ID uniqueidentifier DEFAULT newsequentialid() NOT NULL,
	ScanCode nvarchar(255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	DateCreated datetime NULL,
	CONSTRAINT PK_Workorders PRIMARY KEY (ID)
);
 CREATE NONCLUSTERED INDEX idxLinkID ON MicrovellumData.dbo.Workorders (  LinkID ASC  )  
	 WITH (  PAD_INDEX = OFF ,FILLFACTOR = 100  ,SORT_IN_TEMPDB = OFF , IGNORE_DUP_KEY = OFF , STATISTICS_NORECOMPUTE = OFF , ONLINE = OFF , ALLOW_ROW_LOCKS = ON , ALLOW_PAGE_LOCKS = ON  )
	 ON [PRIMARY ] ;


