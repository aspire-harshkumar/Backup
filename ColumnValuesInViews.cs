using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Motive.MFiles.API.Framework;
using Motive.MFiles.vNextUI.PageObjects;
using Motive.MFiles.vNextUI.Utilities;
using NUnit.Framework;
using NUnit.Framework.Internal;

namespace Motive.MFiles.vNextUI.Tests
{
	[Order( -5 )]
	[Parallelizable( ParallelScope.Self )]
	class ColumnValuesInViews
	{
		/// <summary>
		/// Test class identifier that is used to identify configurations for this class.
		/// </summary>
		protected readonly string classID;

		private string username;
		private string password;
		private string vaultName;

		private TestClassConfiguration configuration;

		private MFilesContext mfContext;

		private TestClassBrowserManager browserManager;


		public ColumnValuesInViews()
		{
			this.classID = "ColumnValuesInViews";
		}

		[OneTimeSetUp]
		public void SetupTestClass()
		{
			// Initialize configurations for the test class based on test context parameters.
			this.configuration = new TestClassConfiguration( this.classID, TestContext.Parameters );

			// Define users required by this test class.
			UserProperties[] users = EnvironmentSetupHelper.GetBasicTestUsers();

			// TODO: Some environment details should probably come from configuration. For example the backend.
			this.mfContext = EnvironmentSetupHelper.SetupEnvironment( EnvironmentHelper.VaultBackend.Firebird, "Data Types And Test Objects.mfb", users );

			this.vaultName = this.mfContext.VaultName;

			// TODO: The "user" identifier here is now defined in SetupHelper. Maybe this should come from configuration and
			// it should also be given to the SetupHelper as parameter.
			this.username = this.mfContext.UsernameOfUser( "user" );
			this.password = this.mfContext.PasswordOfUser( "user" );

			this.browserManager = new TestClassBrowserManager( this.configuration, this.username, this.password, this.vaultName );

		}

		/// <summary>
		/// After all tests have been run, ensure that the browser is closed. Then destroy the 
		/// test environment of the test class.
		/// </summary>
		[OneTimeTearDown]
		public void TeardownTestClass()
		{
			this.browserManager.EnsureQuitBrowser();

			EnvironmentSetupHelper.TearDownEnvironment( this.mfContext );
		}


		/// <summary>
		/// After each test, navigate back to home page if test has passed. But if the test has failed,
		/// then the browser is closed to ensure fresh start for next test.
		/// </summary>
		[TearDown]
		public void EndTest()
		{

			this.browserManager.FinalizeBrowserStateBasedOnTestResult( TestExecutionContext.CurrentContext );
		}

		
		[Test]
		[TestCase(
			"Text properties special characters.xlsx",
			"1. Documents>By Class>Other Document",
			"Keywords",
			"Keywords ÅÄÖåäö !\"#¤%&/()=?`*^_:;<>",
			"modified keywords",
			Description = "Text." )]
		[TestCase(
			"Text properties special characters.xlsx",
			"1. Documents>By Class>Other Document",
			"Description",
			"Description ÅÄÖåäö !\"#¤%&/()=?`*^_:;<>",
			"modified description",
			Description = "Multi-line text." )]
		[TestCase(
			"Department SSLU special characters.txt",
			"1. Documents>By Class Group and Class>4. MEETINGS>Memo",
			"Department",
			"Department ÅÄÖåäö !\"#¤%&/()=?`*^_:;<>",
			"Sales",
			Description = "Value list SSLU." )]
		[TestCase(
			"Country special characters",
			"2. Manage Customers",
			"Country",
			"Country ÅÄÖåäö !\\\"#¤%&/()=?`*^_:;<>",
			"Canada",
			Description = "Value list MSLU." )]
		[TestCase(
			"Object type SSLU special characters.pptx",
			"1. Documents>By Class>Meeting Minutes",
			"Customer SSLU",
			"Customer ÅÄÖåäö !\"#¤%&/()=?`*^_:;<>",
			"CBH International",
			Description = "Object type SSLU." )]
		[TestCase(
			"Logo Design / ESTT",
			"3. Manage Projects>Filter by Customer>ESTT Corporation (IT)",
			"Contact person",
			"Sandra Williams",
			"Daniel Hall",
			Description = "Object type MSLU." )]
		[TestCase(
			"Project Plan / Records Management.doc",
			"1. Documents>By Project>Development of Legal Records Management Application",
			"Document date",
			"7/9/2007",
			"12/14/2018",
			Description = "Date." )]
		[TestCase(
			"EditSinglePropertyInOneObject integer.bmp",
			"1. Documents>Pictures",
			"Integer property",
			"219000",
			"-60985",
			Description = "Integer number." )]
		[TestCase(
			"EditPropertiesOfAllTypesInOneObject.docx",
			"1. Documents>By Customer and Class>CBH International>Other Document",
			"Real number property",
			"195.72",
			"-2964.93",
			Description = "Real number." )]
		[TestCase(
			"Proposal 7701 - City of Chicago (Planning and Development).doc",
			"1. Documents>By Project and Class>Philo District Redevelopment>Proposal",
			"Accepted",
			"No",
			"Yes",
			Description = "Boolean with value No." )]
		[TestCase(
			"Proposal 7728 - Reece, Murphy and Partners.doc",
			"1. Documents>By Class>Proposal",
			"Accepted",
			"Yes",
			"No",
			Description = "Boolean with value Yes." )]
		[TestCase(
			"EditSinglePropertyInOneObject time.txt",
			"1. Documents>By Class>Memo",
			"Time property",
			"2:58 PM",
			"7:06 AM",
			Description = "Time." )]
		public void ModifyColumnValueWhenInView(
			string objectName,
			string viewPath,
			string property,
			string expectedPropertyValue,
			string modifiedValue )
		{

			HomePage homePage = this.browserManager.StartTestAtHomePage();

			// Go to a view.
			ListView listing = homePage.ListView.NavigateToView( viewPath );

			// Insert column to search results view.
			listing.Columns.InsertColumnByContextMenu( property );

			// Get the value in the specified column.
			string actualColumnValue = listing.Columns.GetColumnValueOfObject( objectName, property );

			// Assert that the value is displayed as expected.
			Assert.AreEqual( expectedPropertyValue, actualColumnValue,
				$"Mismatch between expected and actual value in column '{property}'." );

			MetadataCardRightPane mdCard = listing.SelectObject( objectName );
			mdCard.Properties.SetPropertyValue( property, modifiedValue );
			mdCard.SaveAndDiscardOperations.Save();

			// Get the value in the specified column.
			string actualModifiedColumnValue = listing.Columns.GetColumnValueOfObject( objectName, property );

			Assert.AreEqual( modifiedValue, actualModifiedColumnValue,
				$"Mismatch between expected and actual value in column '{property}'." );

			// Remove the column.
			listing.Columns.RemoveColumnByContextMenu( property );
		}
	}
}
