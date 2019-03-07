using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MFilesAPI;
using Motive.MFiles.API.Framework;
using Motive.MFiles.vNextUI.PageObjects;
using Motive.MFiles.vNextUI.PageObjects.MetadataCard;
using Motive.MFiles.vNextUI.Utilities;
using Motive.MFiles.vNextUI.Utilities.AssertHelpers;
using Motive.MFiles.vNextUI.Utilities.GeneralHelpers;
using NUnit.Framework;
using NUnit.Framework.Internal;

namespace Motive.MFiles.vNextUI.Tests
{
	[Order( -12 )]
	[Parallelizable( ParallelScope.Self )]
	public class CheckOutCheckIn
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

		public CheckOutCheckIn()
		{
			this.classID = "CheckOutCheckIn";
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

		[OneTimeTearDown]
		public void TeardownTestClass()
		{
			this.browserManager.EnsureQuitBrowser();

			EnvironmentSetupHelper.TearDownEnvironment( this.mfContext );
		}


		[TearDown]
		public void EndTest()
		{

			this.browserManager.FinalizeBrowserStateBasedOnTestResult( TestExecutionContext.CurrentContext );
		}


		/// <summary>
		/// Testing that the green icons in listing and metadata card are displayed, and basic information such as version and
		/// last modified timestamp are displayed correctly in the metadata card header. All information is verified after checkout
		/// and then after check in.
		/// </summary>
		[Test]
		[Category( "Smoke" )]
		[TestCase( "Corporate presentation.pptx", "Keywords", "test" )]
		public void CheckoutCheckInCurrentUserListingAndMDCardHeaderBasics( string objectName, string property, string propValue )
		{
			HomePage homePage = this.browserManager.StartTestAtHomePage();

			ListView listing = homePage.SearchPane.QuickSearch( objectName );

			MetadataCardRightPane mdCard = listing.SelectObject( objectName );

			// Get the version of the object before checkout.
			int initialVersionBeforeCheckout = mdCard.Header.Version;

			// Get the current system time just before checking out the object.
			string currentDateTimeAtCheckout = TimeHelper.GetCurrentTime();

			mdCard = listing.RightClickItemOpenContextMenu( objectName ).CheckOutObject();

			// The expected new version after checkout is increased by 1 from the initial version.
			int expectedNewVersion = initialVersionBeforeCheckout + 1;

			// Assert that the object is displayed as checked out with green overlay icon in both the list view and in the metadata card.
			Assert.AreEqual( CheckOutStatus.CheckedOutToCurrentUser, listing.GetObjectCheckOutStatus( objectName ) );
			Assert.AreEqual( CheckOutStatus.CheckedOutToCurrentUser, mdCard.Header.CheckOutStatus );

			// Assert user and timestamp details/properties in metadata card header after checkout. Note that here the user
			// is the same for the "last modified by" and "checked out to" properties. Also, the timestamp is the same for "last modified" 
			// and "checked out" properties when the object is checked out to current user.
			MetadataCardAssertHelper.AssertCheckedOutObjectMDCardHeaderInfo( expectedNewVersion, this.username, currentDateTimeAtCheckout,
				this.username, currentDateTimeAtCheckout, mdCard.Header );

			// Pop out the metadata card and make the same assertions as for right pane about icon and header for checked out object.
			MetadataCardPopout popMDCard = mdCard.PopoutMetadataCard();
			Assert.AreEqual( CheckOutStatus.CheckedOutToCurrentUser, popMDCard.Header.CheckOutStatus );
			MetadataCardAssertHelper.AssertCheckedOutObjectMDCardHeaderInfo( expectedNewVersion, this.username, currentDateTimeAtCheckout,
				this.username, currentDateTimeAtCheckout, popMDCard.Header );

			// Close popped out metadata card and continue with the right pane metadata card.
			mdCard = popMDCard.CloseButtonClick();

			// Get the current time just before saving the object. The last modified timestamp is
			// updated when object is saved.
			string currentDateTimeAtSaving = TimeHelper.GetCurrentTime();

			// Modify the object properties and save.
			mdCard.Properties.SetPropertyValue( property, propValue );
			mdCard = mdCard.SaveAndDiscardOperations.Save();

			mdCard = listing.RightClickItemOpenContextMenu( objectName ).CheckInObject();

			// Assert that the object is displayed as checked in with no overlay icon in both the list view and in the metadata card.
			Assert.AreEqual( CheckOutStatus.CheckedIn, listing.GetObjectCheckOutStatus( objectName ) );
			Assert.AreEqual( CheckOutStatus.CheckedIn, mdCard.Header.CheckOutStatus );

			// Assert the user and timestamp details after check in. Also verifies that the checked out information is no longer displayed.
			MetadataCardAssertHelper.AssertCheckedInObjectMDCardHeaderInfo( expectedNewVersion, this.username, currentDateTimeAtSaving, mdCard.Header );

			// Pop out the metadata card and make the same assertions as for right pane about icon and header for checked in object.
			popMDCard = mdCard.PopoutMetadataCard();
			Assert.AreEqual( CheckOutStatus.CheckedIn, popMDCard.Header.CheckOutStatus );
			MetadataCardAssertHelper.AssertCheckedInObjectMDCardHeaderInfo( expectedNewVersion, this.username, currentDateTimeAtSaving, popMDCard.Header );

			popMDCard.CloseButtonClick();
		}

		/// <summary>
		/// Correct red icons are shown in user interface when object is checked out to other user than the current user.
		/// Also checking basic information such as version and Last modified timestamp are displayed correctly in the metadata card header.
		/// </summary>
		[Test]
		[Category( "Smoke" )]
		[TestCase( "Power Line Test Results.doc", "Keywords", "edit" )]
		public void CheckedOutOtherUserListingAndMDCardHeaderBasics( string objectName, string property, string propValue )
		{
			HomePage homePage = this.browserManager.StartTestAtHomePage();

			ListView listing = homePage.SearchPane.QuickSearch( objectName );

			MetadataCardRightPane mdCard = listing.SelectObject( objectName );

			// Get the initial information from metadata card header before checkout.
			int initialVersion = mdCard.Header.Version;
			string initialLastModifiedTimestamp = mdCard.Header.LastModifiedTimestamp;
			string initialLastModifiedByUser = mdCard.Header.LastModifiedBy;

			// Get current time just before the checkout.
			string currentDateTimeAtCheckOut = TimeHelper.GetCurrentTime();

			// Edit metadata and save.
			mdCard = listing.RightClickItemOpenContextMenu( objectName ).CheckOutObject();
			mdCard.Properties.SetPropertyValue( property, propValue );
			mdCard.SaveAndDiscardOperations.Save();

			// Logout the current user.
			LoginPage loginPage = homePage.TopPane.Logout();

			// Login as other user.
			homePage = loginPage.Login( mfContext.UsernameOfUser( "vaultadmin" ), mfContext.PasswordOfUser( "vaultadmin" ), this.vaultName );

			// Locate and select the same object that the other user checked out.
			listing = homePage.SearchPane.QuickSearch( objectName );
			mdCard = listing.SelectObject( objectName );

			// Assert that the object is displayed as checked out to other user with red overlay icon in both 
			// the list view and in the metadata card.
			Assert.AreEqual( CheckOutStatus.CheckedOutToOtherUser, listing.GetObjectCheckOutStatus( objectName ) );
			Assert.AreEqual( CheckOutStatus.CheckedOutToOtherUser, mdCard.Header.CheckOutStatus );

			// Assert that the version, last modified user, and last modified timestamp are displayed as in the latest
			// saved version in server. Also assert the checked out to user corresponds to the previously logged in user
			// and that the timestamp is correct.
			MetadataCardAssertHelper.AssertCheckedOutObjectMDCardHeaderInfo( initialVersion, initialLastModifiedByUser, initialLastModifiedTimestamp,
				this.username, currentDateTimeAtCheckOut, mdCard.Header );

			// Pop out the metadata card and make the same assertions as for right pane about icon and header for checked out object.
			MetadataCardPopout popMDCard = mdCard.PopoutMetadataCard();
			Assert.AreEqual( CheckOutStatus.CheckedOutToOtherUser, popMDCard.Header.CheckOutStatus );
			MetadataCardAssertHelper.AssertCheckedOutObjectMDCardHeaderInfo( initialVersion, initialLastModifiedByUser, initialLastModifiedTimestamp,
				this.username, currentDateTimeAtCheckOut, popMDCard.Header );

			popMDCard.CloseButtonClick();

			homePage.TopPane.Logout();

			this.browserManager.EnsureQuitBrowser();

		}

		/// <summary>
		/// Checkout object and check in without modifications. Verifying that overlay icons are not displayed after the object
		/// is checked in and that the header displays correct information. For example, the version and last modified details
		/// should be "reverted" after check in without changes.
		/// </summary>
		[Test]
		[Category( "Smoke" )]
		[TestCase( "Sales Invoice 312 - CBH International.xls" )]
		public void CheckInWithoutModificationListingAndMDCardHeaderBasics( string objectName )
		{
			HomePage homePage = this.browserManager.StartTestAtHomePage();

			ListView listing = homePage.SearchPane.QuickSearch( objectName );

			MetadataCardRightPane mdCard = listing.SelectObject( objectName );

			// Get the initial information from metadata card header before checkout.
			int initialVersion = mdCard.Header.Version;
			string initialLastModifiedTimestamp = mdCard.Header.LastModifiedTimestamp;
			string initialLastModifiedByUser = mdCard.Header.LastModifiedBy;

			listing.RightClickItemOpenContextMenu( objectName ).CheckOutObject();

			// Check in the object without modification.
			mdCard = listing.RightClickItemOpenContextMenu( objectName ).CheckInObject();

			// Assert that the object is displayed as checked in with no overlay icon in both the list view and in the metadata card.
			Assert.AreEqual( CheckOutStatus.CheckedIn, listing.GetObjectCheckOutStatus( objectName ) );
			Assert.AreEqual( CheckOutStatus.CheckedIn, mdCard.Header.CheckOutStatus );

			// Assert that the object displays the same information in metadata card header as before checkout because the
			// object was not modified.
			MetadataCardAssertHelper.AssertCheckedInObjectMDCardHeaderInfo( initialVersion, initialLastModifiedByUser, initialLastModifiedTimestamp, mdCard.Header );

			// Pop out the metadata card and make the same assertions as for right pane about icon and header for checked in object.
			MetadataCardPopout popMDCard = mdCard.PopoutMetadataCard();
			Assert.AreEqual( CheckOutStatus.CheckedIn, popMDCard.Header.CheckOutStatus );
			MetadataCardAssertHelper.AssertCheckedInObjectMDCardHeaderInfo( initialVersion, initialLastModifiedByUser, initialLastModifiedTimestamp, popMDCard.Header );

			popMDCard.CloseButtonClick();
		}

		/// <summary>
		/// Green icons disappear when undo check out is used on a checked out object. Also checking basic information such as version and
		/// last modified timestamp are displayed correctly in the metadata card header after the undo checkout.
		/// </summary>
		[Test]
		[Category( "Smoke" )]
		[TestCase( "Order - RMP.doc", "Document date", "11/11/2011" )]
		public void UndoCheckoutCurrentUserListingAndMDCardHeaderBasics( string objectName, string property, string propValue )
		{
			HomePage homePage = this.browserManager.StartTestAtHomePage();

			ListView listing = homePage.SearchPane.QuickSearch( objectName );

			MetadataCardRightPane mdCard = listing.SelectObject( objectName );

			// Get the initial information from metadata card header before checkout.
			int initialVersion = mdCard.Header.Version;
			string initialLastModifiedTimestamp = mdCard.Header.LastModifiedTimestamp;
			string initialLastModifiedByUser = mdCard.Header.LastModifiedBy;

			mdCard = listing.RightClickItemOpenContextMenu( objectName ).CheckOutObject();

			// Modify object properties and save.
			mdCard.Properties.SetPropertyValue( property, propValue );
			mdCard = mdCard.SaveAndDiscardOperations.Save();

			mdCard = listing.RightClickItemOpenContextMenu( objectName ).UndoCheckOutObject();

			// Assert that the object is displayed as checked in with no overlay icon in both the list view and in the metadata card.
			Assert.AreEqual( CheckOutStatus.CheckedIn, listing.GetObjectCheckOutStatus( objectName ) );
			Assert.AreEqual( CheckOutStatus.CheckedIn, mdCard.Header.CheckOutStatus );

			// Assert that the version, last modified by, and last modified timestamps are the same as initially before the checkout
			// after the undo checkout is done.
			MetadataCardAssertHelper.AssertCheckedInObjectMDCardHeaderInfo( initialVersion, initialLastModifiedByUser, initialLastModifiedTimestamp, mdCard.Header );

			// Pop out the metadata card and make the same assertions as for right pane about icon and header for checked in object.
			MetadataCardPopout popMDCard = mdCard.PopoutMetadataCard();
			Assert.AreEqual( CheckOutStatus.CheckedIn, popMDCard.Header.CheckOutStatus );
			MetadataCardAssertHelper.AssertCheckedInObjectMDCardHeaderInfo( initialVersion, initialLastModifiedByUser, initialLastModifiedTimestamp, popMDCard.Header );

			popMDCard.CloseButtonClick();
		}


		/// <summary>
		/// Object is visible in checked out to me view when checked out and it disappears when checked in.
		/// </summary>
		[Test]
		[Category( "Smoke" )]
		[TestCase(
			"Parrot.jpg",
			"1. Documents>Pictures",
			"Customer",
			"A&A Consulting (AEC)" )]
		public void ObjectVisibilityInCheckedOutToMeView(
			string objectName,
			string view,
			string property,
			string propValue )
		{

			HomePage homePage = this.browserManager.StartTestAtHomePage();

			ListView listing = homePage.ListView.NavigateToView( view );

			listing.RightClickItemOpenContextMenu( objectName ).CheckOutObject();

			// Go back to home page after checking out the object.
			homePage = homePage.TopPane.TabButtons.HomeTabClick();

			homePage.ListView.GroupingHeaders.ExpandGroup( "Other Views" );
			listing = homePage.ListView.NavigateToView( "Checked Out to Me" );

			// Checked out object is visible in "Checked Out to Me" view.
			Assert.True( listing.IsItemInListing( objectName ) );
			Assert.AreEqual( CheckOutStatus.CheckedOutToCurrentUser, listing.GetObjectCheckOutStatus( objectName ) );

			// Search for the object again.
			listing = homePage.SearchPane.QuickSearch( objectName );

			MetadataCardRightPane mdCard = listing.SelectObject( objectName );

			// Modify the object and save before checking it in.
			mdCard.Properties.SetPropertyValue( property, propValue );
			mdCard = mdCard.SaveAndDiscardOperations.Save();

			listing.RightClickItemOpenContextMenu( objectName ).CheckInObject();

			// Go back to home view again after checking in the object in search view.
			homePage.TopPane.TabButtons.HomeTabClick();

			homePage.ListView.GroupingHeaders.ExpandGroup( "Other Views" );
			listing = homePage.ListView.NavigateToView( "Checked Out to Me" );

			// Checked in object is not visible in "Checked Out to Me" view.
			Assert.False( listing.IsItemInListing( objectName ) );
		}

		/// <summary>
		/// Modify properties of checked out object. The changes are saved and displayed in the metadata card
		/// when the object is checked in.
		/// </summary>
		[Test]
		[Category( "Smoke" )]
		[TestCase(
			"City", "Tampere",
			"Keywords", "ÅÄÖåäö <testing>?",
			"EditSinglePropertyInOneObject textprop",
			Description = "Text" )]
		[TestCase(
			"Description", "Testing with text content åäö etc.",
			"Multiline text property", "Adding value",
			"EditSinglePropertyInOneObject multiline-text.docx",
			Description = "Multi-line text" )]
		[TestCase(
			"Department", "Production",
			"Agreement type", "Project Agreement",
			"EditSinglePropertyInOneObject valueList SSLU",
			Description = "Value list SSLU" )]
		[TestCase(
			"Country", "France",
			"Department MSLU", "Sales",
			"EditSinglePropertyInOneObject valueList MSLU",
			Description = "Value list MSLU" )]
		[TestCase(
			"Owner", "Kimberley Miller",
			"Customer SSLU", "ESTT Corporation (IT)",
			"EditSinglePropertyInOneObject objtype SSLU",
			Description = "Object type SSLU" )]
		[TestCase(
			"Customer", "Reece, Murphy and Partners",
			"Employee", "Bill Richards",
			"EditSinglePropertyInOneObject objType MSLU",
			Description = "Object type MSLU" )]
		[TestCase(
			"Document date", "12/6/2018",
			"Event date", "5/21/2027",
			"EditSinglePropertyInOneObject date",
			Description = "Date" )]
		[TestCase(
			"Time property", "09:44:27 PM",
			"Another time prop", "02:01:12 AM",
			"EditSinglePropertyInOneObject time.txt",
			Description = "Time" )]
		[TestCase(
			"Integer property", "45635",
			"Another integer prop", "-987654321",
			"EditSinglePropertyInOneObject integer.bmp",
			Description = "Integer number" )]
		[TestCase(
			"Real number property", "188.77",
			"Another real number prop", "-987654321.09",
			"EditSinglePropertyInOneObject real number.pptx",
			Description = "Real number" )]
		[TestCase(
			"Accepted", "Yes",
			"Boolean property", "No",
			"EditSinglePropertyInOneObject boolean.xlsx",
			Description = "Boolean" )]
		public void CheckoutModifyPropertiesCheckIn(
			string editProp, string editValue,
			string addProp, string addValue,
			string objectName )
		{
			HomePage homePage = this.browserManager.StartTestAtHomePage();

			ListView listing = homePage.SearchPane.QuickSearch( objectName );

			MetadataCardRightPane mdCard =
				listing.RightClickItemOpenContextMenu( objectName ).CheckOutObject();

			// TODO: Maybe 2 different test cases:
			// i) Add/edit value of all different property data types.
			// ii) Add property and edit value of all different property data types.

			// Modify some existing properties
			mdCard.Properties.SetPropertyValue( editProp, editValue );

			// Add some properties and set values to them.
			mdCard.Properties.AddPropertyAndSetValue( addProp, addValue );

			mdCard.SaveAndDiscardOperations.Save();

			mdCard = listing.RightClickItemOpenContextMenu( objectName ).CheckInObject();

			// Assert that the modified properties and their values are displayed after check in.
			Assert.AreEqual( editValue, mdCard.Properties.GetPropertyValue( editProp ) );
			Assert.AreEqual( addValue, mdCard.Properties.GetPropertyValue( addProp ) );
		}

		/// <summary>
		/// Check out the object and modify and add properties. The changes should not be saved and not be
		/// visible in the metadata card after doing undo checkout.
		/// </summary>
		[Test]
		[Category( "Smoke" )]
		[TestCase(
			"Project Meeting Minutes 1/2006.txt",
			"Meeting type", "Board meeting",
			"Department", "Administration" )]
		public void CheckoutModifyPropertiesUndoCheckout(
			string objectName,
			string editProp, string editValue,
			string addProp, string addValue )
		{
			HomePage homePage = this.browserManager.StartTestAtHomePage();

			ListView listing = homePage.SearchPane.QuickSearch( objectName );

			MetadataCardRightPane mdCard =
				listing.RightClickItemOpenContextMenu( objectName ).CheckOutObject();

			// Get original property value before editing.
			string originalValueProp = mdCard.Properties.GetPropertyValue( editProp );

			// Edit the property value.
			mdCard.Properties.SetPropertyValue( editProp, editValue );

			// Add a property and set value to it.
			mdCard.Properties.AddPropertyAndSetValue( addProp, addValue );

			mdCard = mdCard.SaveAndDiscardOperations.Save();

			// Undo checkout.
			mdCard = listing.RightClickItemOpenContextMenu( objectName ).UndoCheckOutObject();

			// Assert that the property value modification was not saved after undo checkout. The value should be 
			// the original value.
			Assert.AreEqual( originalValueProp, mdCard.Properties.GetPropertyValue( editProp ) );

			// Assert that the added property is not in the metadata card after undo checkout.
			Assert.False( mdCard.Properties.IsPropertyInMetadataCard( addProp ) );
		}

		/// <summary>
		/// Check out object and modify metadata. Another user logs in and cannot see the changes of the previous user. The other
		/// user cannot modify the checked out object either.
		/// </summary>
		[Test]
		[Category( "Smoke" )]
		[TestCase(
			"Invitation to Project Meeting.doc",
			"Project", "Office Design",
			"Reply to", "Agenda - Project Meeting 4/2006",
			"Name or title", "Project" )]
		public void CheckedOutOtherUserCannotSeeChangesOrModify(
			string objectName,
			string editProp, string editValue,
			string addProp, string addValue,
			string deniedProp1, string deniedProp2 )
		{
			HomePage homePage = this.browserManager.StartTestAtHomePage();

			ListView listing = homePage.SearchPane.QuickSearch( objectName );

			MetadataCardRightPane mdCard =
				listing.RightClickItemOpenContextMenu( objectName ).CheckOutObject();

			// Get initial property value before modification.
			string expectedOriginalPropValue = mdCard.Properties.GetPropertyValue( editProp );

			// Modify a property, and add a new property and value to it.
			mdCard.Properties.SetPropertyValue( editProp, editValue );
			mdCard.Properties.AddPropertyAndSetValue( addProp, addValue );

			mdCard.SaveAndDiscardOperations.Save();

			// Logout the current user.
			LoginPage loginPage = homePage.TopPane.Logout();

			// Login as other user.
			homePage = loginPage.Login( this.mfContext.UsernameOfUser( "vaultadmin" ),
				this.mfContext.PasswordOfUser( "vaultadmin" ), this.vaultName );

			// Find the same object as the previous user.
			listing = homePage.SearchPane.QuickSearch( objectName );

			mdCard = listing.SelectObject( objectName );

			// Assert that this user does not see modifications of the previous user because object is still checked out.
			Assert.AreEqual( expectedOriginalPropValue, mdCard.Properties.GetPropertyValue( editProp ) );
			Assert.False( mdCard.Properties.IsPropertyInMetadataCard( addProp ) );

			// Assert that properties cannot be modified. Using these 2 properties as sample.
			Assert.False( mdCard.Properties.CanPropertyEditModeBeActivated( deniedProp1 ) );
			Assert.False( mdCard.Properties.CanPropertyEditModeBeActivated( deniedProp2 ) );

			// Assert that properties cannot be modified in popped out metadata card.
			MetadataCardPopout popMDCard = mdCard.PopoutMetadataCard();
			Assert.False( popMDCard.Properties.CanPropertyEditModeBeActivated( deniedProp1 ) );
			Assert.False( popMDCard.Properties.CanPropertyEditModeBeActivated( deniedProp2 ) );

			popMDCard.CloseButtonClick();

			homePage.TopPane.Logout();

			this.browserManager.EnsureQuitBrowser();
		}

		/// <summary>
		/// Correct red icons are shown in user interface when object is checked out to current user but in another client.
		/// Also checking basic information such as version and Last modified timestamp are displayed correctly in the metadata card header.
		/// </summary>
		[Test]
		[Category( "Smoke" )]
		[TestCase(
			"Travel Expenses.xls",
			"Mike Taylor",
			"7/9/2007 4:53 PM",
			5,
			108,
			0 )]
		public void CheckedOutToOtherClientListingAndMDCardHeaderBasics(
			string objectName,
			string expectedUserLastModified,
			string expectedLastModifiedTimestamp,
			int expectedVersion,
			int objectID,
			int objTypeID )
		{

			// Create ObjID object in M-Files API so that the object can be checked out by API client. 
			ObjID objID = new ObjID();
			objID.ID = objectID;
			objID.Type = objTypeID;

			string timestampAtCheckout = TimeHelper.GetCurrentTime();

			// Checkout the object to the current user by using M-Files API.
			var objVersion = mfContext[ "user" ].ObjectOperations.CheckOut( objID );

			// Using computer name to format the checked out platform string.
			string computerName = Environment.MachineName;
			string headerInfo = "[ {0}-API ]";
			string expectedCheckedOutPlatform = String.Format( headerInfo, computerName );

			// Start the UI part of the test.
			HomePage homePage = this.browserManager.StartTestAtHomePage();

			homePage.ListView.GroupingHeaders.ExpandGroup( "Other Views" );

			// Object should be visible in checked out to me view even if checked out to other client.
			ListView listing = homePage.ListView.NavigateToView( "Checked Out to Me" );

			MetadataCardRightPane mdCard = listing.SelectObject( objectName );

			// Assert that the object is displayed as checked out to other user with red overlay icon in both 
			// the list view and in the metadata card. This is expected behaviour when the object is checked out to
			// current user but in another client.
			Assert.AreEqual( CheckOutStatus.CheckedOutToOtherUser, listing.GetObjectCheckOutStatus( objectName ) );
			Assert.AreEqual( CheckOutStatus.CheckedOutToOtherUser, mdCard.Header.CheckOutStatus );

			HeaderInMetadataCard header = mdCard.Header;

			// The version should be displayed as the latest checked in object version.
			Assert.AreEqual( expectedVersion, header.Version );

			// Last modified should display the Last modified by user information from the latest saved version in server.
			Assert.AreEqual( expectedUserLastModified, header.LastModifiedBy );

			// Last modified time should display the Last modified timestamp from the latest saved version in server.
			Assert.AreEqual( expectedLastModifiedTimestamp, header.LastModifiedTimestamp );

			// Format the expected checked out information. It contains the checkout time, user's full name, 
			// and checkout platform. These pieces of information are separated by space characters.
			string expectedCheckedOutInfo = timestampAtCheckout + " " + this.username + " " + expectedCheckedOutPlatform;

			// Checked out information should be displayed correctly: timestamp + username + platform
			Assert.AreEqual( expectedCheckedOutInfo, header.CheckedOutInformation );
		}
	}
}
