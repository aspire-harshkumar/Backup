using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Motive.MFiles.API.Framework;
using Motive.MFiles.vNextUI.PageObjects;
using Motive.MFiles.vNextUI.PageObjects.Listing;
using Motive.MFiles.vNextUI.Utilities;
using Motive.MFiles.vNextUI.Utilities.GeneralHelpers;
using NUnit.Framework;
using NUnit.Framework.Internal;

namespace Motive.MFiles.vNextUI.Tests
{
	[Order( -7 )]
	[Parallelizable( ParallelScope.Self )]
	class RelationshipsInListing
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

		public RelationshipsInListing()
		{
			this.classID = "RelationshipsInListing";
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

			this.username = this.mfContext.UsernameOfUser( "user" );
			this.password = this.mfContext.PasswordOfUser( "user" );

			this.browserManager = new TestClassBrowserManager( this.configuration, this.username, this.password, this.vaultName );
		}

		[OneTimeTearDown]
		public void TeardownTestClass()
		{
			this.browserManager.EnsureQuitBrowser();

			EnvironmentSetupHelper.TearDownEnvironment( mfContext );
		}

		[TearDown]
		public void EndTest()
		{
			// Returns the browser to the home page to be used by the next test or quits the browser if
			// the test failed.
			this.browserManager.FinalizeBrowserStateBasedOnTestResult( TestExecutionContext.CurrentContext );
		}

		/// <summary>
		/// Helper for printing error message when related object is not listed under relationships.
		/// </summary>
		private string FormatMissingRelatedObjectFailureMessage( string relationshipRootObject, string relationshipHeader, string relatedObject )
		{
			return String.Format( "Expected object '{0}' is not listed in relationships of object '{1}' under header '{2}'",
				relatedObject, relationshipRootObject, relationshipHeader );
		}

		/// <summary>
		/// Helper for printing error message when attached file is not listed under relationships.
		/// </summary>
		private string FormatMissingAttachedFileFailureMessage( string relationshipRootObject, string attachedFile )
		{
			return String.Format( "Expected attached file '{0}' is not listed in relationships of object '{1}'",
				attachedFile, relationshipRootObject );
		}

		/// <summary>
		/// Add relationship via a property. Then expand relationships and verify that the relationship is displayed.
		/// </summary>
		[Test]
		[Category( "Smoke" )]
		[TestCase(
			"Project",
			"Projects",
			"Logo Design / ESTT",
			"Staff Meeting Minutes 4/2007.txt",
			"memo",
			Description = "Object with no existing relationships." )]
		[TestCase(
			"Supervisor",
			"Supervisor",
			"Tina Smith",
			"Andy Nash",
			"andy nash employee",
			Description = "Object with existing relationships in other properties." )]
		[TestCase(
			"Project",
			"Projects",
			"Office Design",
			"Invitation to General Meeting 2004",
			"General Meeting",
			Description = "MFD object with files but no other existing relationships." )]
		public void AddRelationship(
			string relationProperty,
			string relationshipHeader,
			string relatedObject,
			string objectName,
			string searchWord )
		{
			HomePage homePage = this.browserManager.StartTestAtHomePage();

			ListView listing = homePage.SearchPane.QuickSearch( searchWord );

			MetadataCardRightPane mdCard = listing.SelectObject( objectName );

			// Add related object to property.
			mdCard.Properties.SetPropertyValue( relationProperty, relatedObject );
			mdCard.SaveAndDiscardOperations.Save();

			RelationshipsTree relationships = listing.GetRelationshipsTreeOfObject( objectName );
			relationships.ExpandRelationships();

			relationships.ExpandRelationshipHeader( relationshipHeader );

			// Assert that the object is added to the relationships.
			Assert.True( relationships.IsObjectInRelationships( relationshipHeader, relatedObject ),
				this.FormatMissingRelatedObjectFailureMessage( objectName, relationshipHeader, relatedObject ) );

			// Select the related object which verifies that the object can be selected and that metadata card opens.
			MetadataCardRightPane relatedObjMDCard = relationships.SelectRelatedObject( relationshipHeader, relatedObject );
		}

		/// <summary>
		/// Add additional relationship via a property that already has a reference to some object. Then expand
		/// relationships and verify that the new relationship is displayed.
		/// </summary>
		[Test]
		[Category( "Smoke" )]
		[TestCase(
			"Contact person",
			"Contact persons",
			"Thomas Smith",
			"Initial Land Survey / Central Plains",
			"3. Manage Projects>Filter by Customer>Lance Smith Engineering (Surveying)",
			1,
			Description = "Add as 2nd value to property." )]
		[TestCase(
			"Customer",
			"Customers",
			"Fortney Nolte Associates",
			"Project Announcement / Austin.pdf",
			"1. Documents>By Customer>RGPP Partnership",
			2,
			Description = "Add as 3rd value to property." )]
		[TestCase(
			"Customer",
			"Customers",
			"Lance Smith Engineering (Surveying)",
			"Floor Plans of the Additional Building",
			"1. Documents>By Class>Drawing",
			1,
			Description = "Add as 2nd value to property for MFD object with files." )]
		public void AddAdditionalRelationshipInMsluProperty(
			string relationProperty,
			string relationshipHeader,
			string relatedObject,
			string objectName,
			string view,
			int index )
		{
			HomePage homePage = this.browserManager.StartTestAtHomePage();

			ListView listing = homePage.ListView.NavigateToView( view );

			MetadataCardRightPane mdCard = listing.SelectObject( objectName );

			// Set reference to another object via value in multi-select lookup. 
			mdCard.Properties.SetMultiSelectLookupPropertyValueByIndex( relationProperty, relatedObject, index );
			mdCard.SaveAndDiscardOperations.Save();

			RelationshipsTree relationships = listing.GetRelationshipsTreeOfObject( objectName );
			relationships.ExpandRelationships();

			relationships.ExpandRelationshipHeader( relationshipHeader );

			// Assert that the object is added to the relationships.
			Assert.True( relationships.IsObjectInRelationships( relationshipHeader, relatedObject ),
				this.FormatMissingRelatedObjectFailureMessage( objectName, relationshipHeader, relatedObject ) );

			// Select the related object which verifies that the object can be selected and that metadata card opens.
			MetadataCardRightPane relatedObjMDCard = relationships.SelectRelatedObject( relationshipHeader, relatedObject );
		}

		/// <summary>
		/// Expand relationships of object. Then add relationship and verify that the related object is displayed
		/// under specific relationship header.
		/// </summary>
		[Test]
		[Category( "Smoke" )]
		[TestCase(
			"Employee",
			"Employees",
			"Mike Taylor",
			"Invitation to Project Meeting 1/2007.doc",
			"Invitation",
			Description = "Object with existing relationships in other properties." )]
		[TestCase(
			"Owner",
			"Owner",
			"Bill Richards",
			"Bill of Materials: Furniture",
			"materials",
			Description = "MFD object with files and existing relationships in other properties." )]
		public void ExpandedRelationshipsAddRelationship(
			string relationProperty,
			string relationshipHeader,
			string relatedObject,
			string objectName,
			string searchWord )
		{
			HomePage homePage = this.browserManager.StartTestAtHomePage();

			ListView listing = homePage.SearchPane.QuickSearch( searchWord );

			// Expand relationships of object.
			RelationshipsTree relationships = listing.GetRelationshipsTreeOfObject( objectName );
			relationships.ExpandRelationships();

			// Select object to open metadata card.
			MetadataCardRightPane mdCard = listing.SelectObject( objectName );

			// Add property and set value to create a relationship to the object via that property.
			mdCard.Properties.AddPropertyAndSetValue( relationProperty, relatedObject );
			mdCard.SaveAndDiscardOperations.Save();

			relationships.ExpandRelationshipHeader( relationshipHeader );

			Assert.True( relationships.IsObjectInRelationships( relationshipHeader, relatedObject ),
				this.FormatMissingRelatedObjectFailureMessage( objectName, relationshipHeader, relatedObject ) );

			// Select the related object which verifies that the object can be selected and that metadata card opens.
			MetadataCardRightPane relatedObjMDCard = relationships.SelectRelatedObject( relationshipHeader, relatedObject );
		}

		/// <summary>
		/// Expand relationships and specific relationship header. Then add additional relationship via a property that already has 
		/// a reference to some object. 
		/// </summary>
		[Test]
		[Category( "Smoke" )]
		[TestCase(
			"Customer",
			"Customers",
			"CBH International",
			"Order - Web Graphics.doc",
			"1. Documents>By Class>Order",
			1,
			Description = "Add as 2nd value to property." )]
		[TestCase(
			"Project",
			"Projects",
			"Philo District Redevelopment",
			"Project Plan",
			"1. Documents>By Project>Office Design",
			1,
			Description = "Add as 2nd value to property for MFD object with files." )]
		public void ExpandedRelationshipsAddAdditionalRelationshipInMsluProperty(
			string relationProperty,
			string relationshipHeader,
			string relatedObject,
			string objectName,
			string view,
			int index )
		{
			HomePage homePage = this.browserManager.StartTestAtHomePage();

			ListView listing = homePage.ListView.NavigateToView( view );

			// Expand relationships of object.
			RelationshipsTree relationships = listing.GetRelationshipsTreeOfObject( objectName );
			relationships.ExpandRelationships();

			// Expand the relationships header with already existing objects.
			relationships.ExpandRelationshipHeader( relationshipHeader );

			// Select object to open metadata card.
			MetadataCardRightPane mdCard = listing.SelectObject( objectName );

			mdCard.Properties.SetMultiSelectLookupPropertyValueByIndex( relationProperty, relatedObject, index );
			mdCard.SaveAndDiscardOperations.Save();

			Assert.True( relationships.IsObjectInRelationships( relationshipHeader, relatedObject ),
				this.FormatMissingRelatedObjectFailureMessage( objectName, relationshipHeader, relatedObject ) );

			// Select the related object which verifies that the object can be selected and that metadata card opens.
			MetadataCardRightPane relatedObjMDCard = relationships.SelectRelatedObject( relationshipHeader, relatedObject );
		}

		/// <summary>
		/// Expand relationships of an object and verify that the relationships tree contains all expected attached files
		/// and related objects.
		/// </summary>
		[Test]
		[Category( "Smoke" )]
		[TestCase(
			"Relationship types test",
			"attached document file.docx,Attached presentation.pptx",
			"Documents:Arch.jpg|Lily.jpg",
			"relationship",
			"Assignment",
			Description = "Assignment with files." )]
		[TestCase(
			"Door Chart 51E",
			"Front view.dwg,List of Parts.txt,Top view.dwg",
			"Projects:Hospital Expansion (Miami, FL);Customers:A&A Consulting (AEC)",
			"Door",
			"Document",
			Description = "MFD with files." )]
		public void ObjectDisplaysAttachedFilesAndRelatedObjects(
			string objectName,
			string attachedFiles,
			string relatedObjects,
			string searchWord,
			string searchFilter )
		{

			// Convert expected attached files from test data string to a list of strings.
			List<string> expectedAttachedFiles = StringSplitHelper.ParseStringToStringList( attachedFiles, ',' );

			// Convert expected related objects from test data string to a dictionary where the relationship
			// header is the key and a list of related objects is the value.
			Dictionary<string, List<string>> expectedRelatedObjectsByHeader =
				StringSplitHelper.ParseStringToStringListsByKey( relatedObjects, ';', ':', '|' );

			HomePage homePage = this.browserManager.StartTestAtHomePage();

			ListView listing = homePage.SearchPane.FilteredQuickSearch( searchWord, searchFilter );

			RelationshipsTree relationships = listing.GetRelationshipsTreeOfObject( objectName );
			relationships.ExpandRelationships();

			// Assert that attached files are shown.
			foreach( string attachedFile in expectedAttachedFiles )
			{
				Assert.True( relationships.IsFileAttached( attachedFile ),
					this.FormatMissingAttachedFileFailureMessage( objectName, attachedFile ) );
			}

			// Assert that all related objects are shown.
			foreach( string relationshipHeader in expectedRelatedObjectsByHeader.Keys )
			{
				relationships.ExpandRelationshipHeader( relationshipHeader );

				foreach( string relatedObject in expectedRelatedObjectsByHeader[ relationshipHeader ] )
				{
					Assert.True( relationships.IsObjectInRelationships( relationshipHeader, relatedObject ),
						this.FormatMissingRelatedObjectFailureMessage( objectName, relationshipHeader, relatedObject ) );
				}
			}
		}

		/// <summary>
		/// Add relationship to an object. Expand the relationships and open relationship header to see the relationship to
		/// the added related object. Then expand relationships of that related object and further expand the relationship header
		/// which should contain the "to relationship" back to the original object.
		/// </summary>
		[Test]
		[Category( "Smoke" )]
		[TestCase(
			"Marketing Meeting Agenda.doc",
			"Contact person",
			"Contact persons",
			"Lewis Hawkins",
			"Documents",
			"agenda",
			"Document" )]
		public void RelationshipRootObjectInRelatedObjectRelationships(
			string objectName,
			string relationProperty,
			string relationshipHeader,
			string relatedObject,
			string toRelationshipHeader,
			string searchWord,
			string searchFilter )
		{
			HomePage homePage = this.browserManager.StartTestAtHomePage();

			ListView listing = homePage.SearchPane.FilteredQuickSearch( searchWord, searchFilter );

			MetadataCardRightPane mdCard = listing.SelectObject( objectName );

			// Add relationship to object.
			mdCard.Properties.AddPropertyAndSetValue( relationProperty, relatedObject );
			mdCard.SaveAndDiscardOperations.Save();

			// Expand relationships.
			RelationshipsTree relationships = listing.GetRelationshipsTreeOfObject( objectName );
			relationships.ExpandRelationships();

			// Expand relationship header containing the added relationship.
			relationships.ExpandRelationshipHeader( relationshipHeader );

			// Select the related object which verifies that the object can be selected and that metadata card opens.
			relationships.SelectRelatedObject( relationshipHeader, relatedObject );

			// Expand the relationships of the related object.
			RelationshipsTree relationshipsOfRelatedObject =
				relationships.GetRelationshipsOfRelatedObject( relationshipHeader, relatedObject );

			relationshipsOfRelatedObject.ExpandRelationships();

			// Expand the relationship header of the related object. It should contain the reference back to the original object.
			relationshipsOfRelatedObject.ExpandRelationshipHeader( toRelationshipHeader );

			// Assert that "to relationship" back to the original object is displayed under the related object.
			Assert.True( relationshipsOfRelatedObject.IsObjectInRelationships( toRelationshipHeader, objectName ),
				this.FormatMissingRelatedObjectFailureMessage( relatedObject, toRelationshipHeader, objectName ) );

			// Select the original object which verifies that the object can be selected and that metadata card opens.
			MetadataCardRightPane relatedObjMDCard = relationshipsOfRelatedObject.SelectRelatedObject( toRelationshipHeader, objectName );
		}

		/// <summary>
		/// Add relationship to an object. Then search for the related object and expand its relationships.
		/// Verify that the relationships tree contains "to relationship" of the original object.
		/// </summary>
		[Test]
		[Category( "Smoke" )]
		[TestCase(
			"DAT Sports & Entertainment",
			"Owner",
			"Tina Smith",
			"Customers",
			"2. Manage Customers",
			"Employee" )]
		public void ToRelationshipIsDisplayed(
			string objectName,
			string relationProperty,
			string relatedObject,
			string toRelationshipHeader,
			string view,
			string searchFilter )
		{
			HomePage homePage = this.browserManager.StartTestAtHomePage();

			ListView listing = homePage.ListView.NavigateToView( view );

			MetadataCardRightPane mdCard = listing.SelectObject( objectName );

			// Add relationship.
			mdCard.Properties.AddPropertyAndSetValue( relationProperty, relatedObject );
			mdCard.SaveAndDiscardOperations.Save();

			// Search for the object that was set to relationships.
			listing = homePage.SearchPane.FilteredQuickSearch( relatedObject, searchFilter );

			// Expand relationships.
			RelationshipsTree relationships = listing.GetRelationshipsTreeOfObject( relatedObject );
			relationships.ExpandRelationships();

			// Expand the relationship header of the related object. It should contain the reference back to the original object.
			relationships.ExpandRelationshipHeader( toRelationshipHeader );

			// Assert that "to relationship" back to the original object is displayed under the related object.
			Assert.True( relationships.IsObjectInRelationships( toRelationshipHeader, objectName ),
				this.FormatMissingRelatedObjectFailureMessage( relatedObject, toRelationshipHeader, objectName ) );

			// Select the original object which verifies that the object can be selected and that metadata card opens.
			MetadataCardRightPane relatedObjMDCard = relationships.SelectRelatedObject( toRelationshipHeader, objectName );
		}

		/// <summary>
		/// Remove specific object from MSLU property and verify that the object is not visible under the relationships.
		/// This tests situation when one related object is removed but still some object(s) remain under that same relationship header.
		/// </summary>
		[Test]
		[Category( "Smoke" )]
		[TestCase(
			"Project Announcement / Austin.pdf",
			"Customer",
			"Customers",
			"RGPP Partnership",
			"Austin",
			"Document" )]
		public void RemoveRelationshipFromMsluProperty(
			string objectName,
			string relationProperty,
			string relationshipHeader,
			string relatedObject,
			string searchWord,
			string searchFilter )
		{
			HomePage homePage = this.browserManager.StartTestAtHomePage();

			ListView listing = homePage.SearchPane.FilteredQuickSearch( searchWord, searchFilter );

			MetadataCardRightPane mdCard = listing.SelectObject( objectName );

			// Remove specific object from relationships.
			mdCard.Properties.RemoveMultiSelectLookupPropertyValue( relationProperty, relatedObject );
			mdCard.SaveAndDiscardOperations.Save();

			RelationshipsTree relationships = listing.GetRelationshipsTreeOfObject( objectName );
			relationships.ExpandRelationships();

			relationships.ExpandRelationshipHeader( relationshipHeader );

			// Assert that the object is not listed anymore in the relationships.
			Assert.False( relationships.IsObjectInRelationships( relationshipHeader, relatedObject ) );
		}

		/// <summary>
		/// Expand relationships and specific relationship header. Then remove property which contained relationships in the expanded
		/// relationship header. Verify that the relationship header disappears after saving metadata changes.
		/// </summary>
		[Test]
		[Category( "Smoke" )]
		[TestCase(
			"Project Schedule.pdf",
			"Customer",
			"Customers",
			"schedule",
			"Document" )]
		public void ExpandedRelationshipsRemoveRelationshipProperty(
			string objectName,
			string relationProperty,
			string relationshipHeader,
			string searchWord,
			string searchFilter )
		{
			HomePage homePage = this.browserManager.StartTestAtHomePage();

			ListView listing = homePage.SearchPane.FilteredQuickSearch( searchWord, searchFilter );

			// Expand relationships tree and the specified relationship header before modifying object metadata.
			RelationshipsTree relationships = listing.GetRelationshipsTreeOfObject( objectName );
			relationships.ExpandRelationships();

			relationships.ExpandRelationshipHeader( relationshipHeader );

			MetadataCardRightPane mdCard = listing.SelectObject( objectName );

			// Remove property which contains the related objects of the expanded relationship header.
			mdCard.Properties.RemoveProperty( relationProperty );
			mdCard.SaveAndDiscardOperations.Save();

			// Assert that relationship header disappears from relationships tree after relationship property 
			// is removed from metadata.
			Assert.False( relationships.IsRelationshipHeaderInRelationships( relationshipHeader ) );

		}
	}
}
