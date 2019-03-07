using System;
using System.Collections.Generic;
using System.Linq;
using MFilesAPI;
using Motive.MFiles.API.Framework;
using Motive.MFiles.vNextUI.PageObjects;
using Motive.MFiles.vNextUI.Utilities;
using Motive.MFiles.vNextUI.Utilities.GeneralHelpers;
using NUnit.Framework;
using NUnit.Framework.Internal;

namespace Motive.MFiles.vNextUI.Tests
{
	[Order( -15 )]
	[Parallelizable( ParallelScope.Self )]
	class Views
	{
		/// <summary>
		/// Test class identifier that is used to identify configurations for this class.
		/// </summary>
		protected readonly string classID;

		private string username;
		private string password;
		private string vaultName;

		/// <summary>
		/// Assert messages for additional info.
		/// </summary>
		private static readonly string BreadCrumbItemMismatchMessage =
			"Mismatch between the expected and actual breadcrumb item.";
		private static readonly string ViewConditionMismatchMessage =
			"Mismatch between the expected view condition and object property value.";
		private static readonly string ListViewItemsCountMismatchMessage =
			"Mismatch between the expected and actual list view items count.";
		private static readonly string ItemNotFoundMessage =
			"Expected item '{0}' is not found in listing.";
		private static readonly string ItemFoundMessage =
			"Item '{0}' is found in listing when it was not expected.";

		private TestClassConfiguration configuration;

		private MFilesContext mfContext;

		private TestClassBrowserManager browserManager;

		public Views()
		{
			this.classID = "Views";
		}

		[OneTimeSetUp]
		public void SetupTestClass()
		{
			// Initialize configurations for the test class based on test context parameters.
			this.configuration = new TestClassConfiguration( this.classID, TestContext.Parameters );

			// Define users required by this test class.
			UserProperties[] users = EnvironmentSetupHelper.GetDifferentTestUsers();

			// TODO: Some environment details should probably come from configuration. For example the back end.
			this.mfContext = EnvironmentSetupHelper.SetupEnvironment( EnvironmentHelper.VaultBackend.Firebird, "Views Vault.mfb", users );

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

		private string FormatItemNotFoundAssertionMessage( string itemName )
		{
			return string.Format( ItemNotFoundMessage, itemName );
		}

		private string FormatItemFoundAssertionMessage( string itemName )
		{
			return string.Format( ItemFoundMessage, itemName );
		}

		/// <summary>
		/// A wrapper method for getting the current method name.
		/// </summary>
		/// <returns>Current method name.</returns>
		private string GetCurrentMethodName()
		{
			return NUnit.Framework.TestContext.CurrentContext.Test.MethodName;
		}

		/// <summary>
		/// Frames the assignment name based on the current method name and date & time.
		/// </summary>
		/// <returns>Assignment name which is combined with Method name and current date & time.</returns>
		private string GetObjectName()
		{
			string objName = this.GetCurrentMethodName() + "-" + TimeHelper.GetCurrentDateAndTime( TimeHelper.TimeFormat.CustomFormat, "yyyy/MM/dd HH:mm:ss" );
			objName = objName.Replace( "/", "" ).Replace( ":", "" ).Replace( " ", "_" );
			return objName;
		}

		/// <summary>
		/// Removes the grouping level from the view.
		/// </summary>		
		/// <param name="viewName"></param>
		/// <param name="groupingLevel">Start from 1.</param>
		public void RemoveGroupingLevelInView( string viewName, int groupingLevel )
		{
			// Get the vault.
			var vault = this.mfContext[ "vaultadmin" ];

			// Variable to declare whether the grouping level is removed or not.
			bool removed = false;

			// Get the available  views.
			var views = vault.ViewOperations.GetViews( MFViewCategory.MFViewCategoryNormal );

			// Iterate through the each view to get the specified view.
			foreach( View view in views )
			{
				// Check for the specified view.
				if( view.Name != null && view.Name.Equals( viewName ) )
				{
					// Check if mentioned grouping level is higher than the actual level, then break the action. 
					if( groupingLevel > view.Levels.Count )
						break;

					// Remove the specific grouping level from the view.
					view.Levels.Remove( groupingLevel );

					// Update the view settings.
					vault.ViewOperations.UpdateView( view );

					// Update the variable to declare the view is updated.
					removed = true;

					// Breaking the loop.
					break;
				}
			}

			// Check and throw exception if the view is not updated.
			if( !removed )
				throw new Exception( "Grouping level '" + groupingLevel + "' is not removed from the '" + viewName + "'." );

		} // end RemoveGroupingLevelInView

		/// <summary>
		/// Testing that the common view can have empty virtual folders 
		/// for the values which don't have the matching objects.
		/// </summary>
		[Test]
		[Order( 1 )]
		[TestCase( "FirstNavigation View>Assignment", Category = "Smoke" )]
		public void GroupingLevelWithEmptyFolders( string pathToNavigate )
		{

			// Starts the test at HomePage as default user.
			HomePage homePage = this.browserManager.StartTestAtHomePage();

			// Navigate to empty virtual folder.
			ListView listing = homePage.ListView.NavigateToView( pathToNavigate );

			// Assert that the virtual folder is empty.
			Assert.AreEqual( 0, listing.NumberOfItems, ListViewItemsCountMismatchMessage );

			// Navigate to home view.
			homePage.TopPane.TabButtons.HomeTabClick();

			// Creating an object to verify that the previously navigated empty virtual folder will have content.
			MetadataCardPopout newObjMDCard = homePage.TopPane.CreateNewObject( "Assignment" );

			// Set the name and assignee.
			string objName = this.GetObjectName();
			newObjMDCard.Properties.SetPropertyValue( "Class", "Assignment", 5 );
			newObjMDCard.Properties.SetPropertyValue( "Name or title", objName );
			newObjMDCard.Properties.SetPropertyValue( "Assigned to", this.username );

			// Create the object.
			newObjMDCard.SaveAndDiscardOperations.Save( false );

			// Navigate to the virtual folder which is previously empty.
			listing = homePage.ListView.NavigateToView( pathToNavigate );

			// Assert that the virtual folder is not empty after creating an object.
			Assert.AreEqual( 1, listing.NumberOfItems, ListViewItemsCountMismatchMessage );

		} // end GroupingLevelWithEmptyFolders

		/// <summary>
		/// Testing that the common view should not have the empty virtual folders 
		/// when Show empty folders is unchecked.
		/// </summary>
		[Test]
		[Order( 2 )]
		[TestCase(
			"FirstNavigation View>Document",
			"Other Administrative Document",
			4, Category = "Smoke" )]
		public void GroupingLevelWithOutEmptyFolders(
			string pathToNavigate,
			string classValue,
			int expectedPropertyCountAfterSettingClass )
		{

			// Starts the test at HomePage as default user.
			HomePage homePage = this.browserManager.StartTestAtHomePage();

			// Navigate to the common view.
			ListView listing = homePage.ListView.NavigateToView( pathToNavigate );

			// Assert that the empty virtual folder doesn't exist in the list view.
			Assert.False( listing.IsItemInListing( classValue ),
				this.FormatItemFoundAssertionMessage( classValue ) );

			// Navigate to home view.
			homePage.TopPane.TabButtons.HomeTabClick();

			// Creating an object to verify that the previously not exists virtual folder will be displayed in the view.
			TemplateSelectorDialog templateSelector = homePage.TopPane.CreateNewObjectFromTemplate( "Document" );

			// Filter to see blank templates and select the template.
			templateSelector.SetTemplateFilter( TemplateSelectorDialog.TemplateFilter.Blank );
			templateSelector.SelectTemplate( "Text Document (.txt)" );

			// Click next button to proceed to the metadata card of the new object.
			MetadataCardPopout newObjMDCard = templateSelector.NextButtonClick();

			// Set the class and name.
			string objName = this.GetObjectName();
			newObjMDCard.Properties.SetPropertyValue( "Class", classValue, expectedPropertyCountAfterSettingClass );
			newObjMDCard.Properties.SetPropertyValue( "Name or title", objName );
			newObjMDCard.CheckInImmediatelyClick();

			// Create the object.
			newObjMDCard.SaveAndDiscardOperations.Save( false );

			// Navigate to the common view.
			listing = homePage.ListView.NavigateToView( pathToNavigate );

			// Assert that the virtual folder exists after creating an object.
			Assert.True( listing.IsItemInListing( classValue ),
				this.FormatItemNotFoundAssertionMessage( classValue ) );

		} // end GroupingLevelWithOutEmptyFolders

		/// <summary>
		/// Testing that the grouping level based on the different property type values 
		/// [For e.g.: Integer, Real number, Text, Boolean and Time] in common view.
		/// </summary>
		[Test]
		[Order( 3 )]
		[TestCase(
			"GroupingLevel RealNumberProperty>0.99",
			"GroupingLevel RealNumberProperty",
			"0.99",
			"Window Chart E14.dwg",
			"Document",
			"Real Number",
			"0.78956",
			"0.78956",
			"GroupingLevel RealNumberProperty;0.78956",
			Description = "Real number property", Category = "Smoke" )]
		[TestCase(
			"GroupingLevel RealNumberProperty>3.98111",
			"GroupingLevel RealNumberProperty",
			"3.98111",
			"West Elevation.dwg",
			"Document",
			"Real Number",
			"0.78956",
			"0.78956",
			"GroupingLevel RealNumberProperty;0.78956",
			Description = "Real number property", Category = "Smoke" )]
		[TestCase(
			"GroupingLevel IntegerProperty>-50",
			"GroupingLevel IntegerProperty",
			"-50",
			"Training Slides - Day 4.ppt",
			"Document",
			"Integer",
			"38968",
			"38968",
			"GroupingLevel IntegerProperty;38968",
			Description = "Integer property", Category = "Smoke" )]
		[TestCase(
			"GroupingLevel IntegerProperty>98",
			"GroupingLevel IntegerProperty",
			"98",
			"Training Slides - Day 1.ppt",
			"Document",
			"Integer",
			"-10000",
			"-10000",
			"GroupingLevel IntegerProperty;-10000",
			Description = "Integer property", Category = "Smoke" )]
		[TestCase(
			"GroupingLevel TextProperty>partnership companies",
			"GroupingLevel TextProperty",
			"partnership companies",
			"Invitation to General Meeting 2004",
			"Document",
			"Keywords",
			"partnership companies, Meeting Notice",
			"partnership companies, Meeting Notice",
			"GroupingLevel TextProperty;partnership companies, Meeting Notice",
			Description = "Text Property", Category = "Smoke" )]
		[TestCase(
			"GroupingLevel BooleanProperty>Yes",
			"GroupingLevel BooleanProperty",
			"Yes",
			"Office Design",
			"Project",
			"In progress",
			"No",
			"No",
			"GroupingLevel BooleanProperty;No",
			Description = "Boolean Property", Category = "Smoke" )]
		[TestCase(
			"GroupingLevel BooleanProperty>No",
			"GroupingLevel BooleanProperty",
			"No",
			"Leadership Training / OMCC",
			"Project",
			"In progress",
			"Yes",
			"Yes",
			"GroupingLevel BooleanProperty;Yes",
			Description = "Boolean Property", Category = "Smoke" )]
		[TestCase(
			"GroupingLevel LookupProperty>General meeting",
			"GroupingLevel LookupProperty",
			"General meeting",
			"Invitation to General Meeting 2005.doc",
			"Document",
			"Meeting type",
			"Other meeting",
			"Other meeting",
			"GroupingLevel LookupProperty;Other meeting",
			Description = "Lookup Property", Category = "Smoke" )]
		[TestCase(
			"GroupingLevel TimeProperty>1:01 PM",
			"GroupingLevel TimeProperty",
			"1:01 PM",
			"Sandra Williams",
			"Contact person",
			"Time Property",
			"2:02:02 AM",
			"2:02 AM",
			"GroupingLevel TimeProperty;2:02 AM",
			Description = "Time Property", Category = "Smoke" )]
		[TestCase(
			"GroupingLevel TimeProperty>3:18 PM",
			"GroupingLevel TimeProperty",
			"3:18 PM",
			"Elisabeth Chapman",
			"Contact person",
			"Time Property",
			"12:18:45 PM",
			"12:18 PM",
			"GroupingLevel TimeProperty;12:18 PM",
			Description = "Time Property", Category = "Smoke" )]
		public void GroupingLevelBasedOnDiffPropertyTypes(
			string pathToNavigate,
			string viewName,
			string virtualViewName,
			string objectName,
			string objectType,
			string property,
			string propertyValue,
			string latestVirtualFolderName,
			string expectedBreadCrumbItems )
		{

			// Starts the test at HomePage as default user.
			HomePage homePage = this.browserManager.StartTestAtHomePage();

			// Navigate to the specified folder.
			ListView listing = homePage.ListView.NavigateToView( pathToNavigate );

			// Check whether the expected object is listed in the view.
			Assert.True( listing.IsItemInListing( objectName ),
				this.FormatItemNotFoundAssertionMessage( objectName ) );

			// Navigate to search view.
			listing = homePage.SearchPane.FilteredQuickSearch( objectName, objectType );

			// Select the object in the view.
			MetadataCardRightPane mdCard = homePage.ListView.SelectObject( objectName );

			// Update the object to not match the current view condition.
			mdCard.Properties.SetPropertyValue( property, propertyValue );
			mdCard = mdCard.SaveAndDiscardOperations.Save();

			// Navigate to home view.
			homePage.TopPane.TabButtons.HomeTabClick();

			// Navigate to the specified folder.
			listing = homePage.ListView.NavigateToView( viewName );

			// Check that the property value exists in the view as virtual folder.
			Assert.True( listing.IsItemInListing( latestVirtualFolderName ),
				this.FormatItemNotFoundAssertionMessage( latestVirtualFolderName ) );

			// Navigate to the specified virtual folder.
			listing = homePage.ListView.NavigateToView( latestVirtualFolderName );

			// Check breadcrumb displays the navigated view path.
			List<string> expectedBreadCrumb = ( this.vaultName + ";" + expectedBreadCrumbItems ).Split( ';' ).ToList();
			List<string> actualBreadCrumb = homePage.TopPane.BreadCrumb;

			Assert.AreEqual( expectedBreadCrumb, homePage.TopPane.BreadCrumb, BreadCrumbItemMismatchMessage );

			// Check the object is listed in the view.
			Assert.True( listing.IsItemInListing( objectName ),
				this.FormatItemNotFoundAssertionMessage( objectName ) );

		} // end GroupingLevelBasedOnDiffPropertyTypes				

		/// <summary>
		/// Testing that the common view have all the object types in the view
		/// when Show Documents And Other Objects is enabled.
		/// </summary>
		[Test]
		[Order( 4 )]
		[TestCase(
			"ShowDocumentsAndOtherObjects Enabled>Document",
			"Area map.tif",
			"Document",
			"ShowDocumentsAndOtherObjects Enabled;Document",
			Description = "Document object", Category = "Smoke" )]
		[TestCase(
			"ShowDocumentsAndOtherObjects Enabled>Customer",
			"CBH International",
			"Customer",
			"ShowDocumentsAndOtherObjects Enabled;Customer",
			Description = "Non-document object", Category = "Smoke" )]
		public void CheckShowDocumentsAndOtherObjectsEnabledView(
			string viewToNavigate,
			string objectName,
			string objectType,
			string expectedBreadCrumbItems )
		{

			// Starts the test at HomePage as default user.
			HomePage homePage = this.browserManager.StartTestAtHomePage();

			// Navigate to the specified folder.
			ListView listing = homePage.ListView.NavigateToView( viewToNavigate );

			// Check the object is listed in the view.
			Assert.True( listing.IsItemInListing( objectName ),
				this.FormatItemNotFoundAssertionMessage( objectName ) );

			// Select the object in the view.
			MetadataCardRightPane mdCard = homePage.ListView.SelectObject( objectName );

			// Check the expected object type object is listed in the view.
			Assert.AreEqual( objectType, mdCard.Header.ObjectType, "Mismatch between the expected and actual object type." );

			// Check breadcrumb displays the navigated view path.
			List<string> expectedBreadCrumb = ( this.vaultName + ";" + expectedBreadCrumbItems ).Split( ';' ).ToList();
			List<string> actualBreadCrumb = homePage.TopPane.BreadCrumb;

			Assert.AreEqual( expectedBreadCrumb, actualBreadCrumb, BreadCrumbItemMismatchMessage );

		} // end CheckDocumentObjAndOtherObjTypesInCommonView

		/// <summary>
		/// Testing that the Latest version of object which not matches the filter condition 
		/// but it's older version matches the filter condition should be displayed in the common view
		/// when Show Latest Version is enabled.
		/// </summary>
		[Test]
		[Order( 5 )]
		[TestCase(
			"ShowLatestVersion View",
			"Samuel Lewis",
			"Employee",
			"Department",
			"Sales",
			Category = "Smoke" )]
		public void CheckCommonViewShowsLatVerOfObjEvenIfLatVerNotMatchTheViewCondition(
			string pathToNavigate,
			string objectName,
			string objectType,
			string property,
			string propertyValue )
		{

			// Starts the test at HomePage as default user.
			HomePage homePage = this.browserManager.StartTestAtHomePage();

			// Navigate to the specified folder.
			ListView listing = homePage.ListView.NavigateToView( pathToNavigate );

			// Select the object in the view.
			MetadataCardRightPane mdCard = listing.SelectObject( objectName );

			// Get the filter condition property value from metadatacard.
			string filterConditionValue = mdCard.Properties.GetPropertyValue( property );

			// Navigate to search view.
			listing = homePage.SearchPane.FilteredQuickSearch( objectName, objectType );

			// Select the object in the view.
			mdCard = homePage.ListView.SelectObject( objectName );

			// Update the object to not match the current view condition.
			mdCard.Properties.SetPropertyValue( property, propertyValue );
			mdCard = mdCard.SaveAndDiscardOperations.Save();

			// Navigate to home view.
			homePage.TopPane.TabButtons.HomeTabClick();

			// Navigate to the specified folder.
			listing = homePage.ListView.NavigateToView( pathToNavigate );

			// Check the modified object exists in the view.
			Assert.True( listing.IsItemInListing( objectName ),
				this.FormatItemNotFoundAssertionMessage( objectName ) +
				" [Show latest version enabled view.]" );

			// Select the object in the view.
			mdCard = homePage.ListView.SelectObject( objectName );

			// Get the filter condition property value from metadatacard.
			string actualValue = mdCard.Properties.GetPropertyValue( property );

			// Check that the latest version of object is displayed even when it doesn't have 
			// the matching filter value anymore.
			Assert.AreNotEqual( filterConditionValue, actualValue,
				ViewConditionMismatchMessage + " [Show latest version enabled view.]" );

		} // end CheckCommonViewShowsLatVerOfObjEvenIfLatVerNotMatchTheViewCondition

		/// <summary>
		/// Testing that the Objects which have the empty value for the grouping condition 
		/// should be listed in the same level when Show on same level is enabled.
		/// </summary>
		[Test]
		[Order( 6 )]
		[TestCase(
			"Grouping:ShowOnThisLevel",
			"Request for Proposal - RMP.doc",
			"Document",
			"Accepted",
			"Yes;No",
			Category = "Smoke" )]
		public void CheckObjDisplayedInSameLevelWhenObjHavingEmptyValueForProp(
			string pathToNavigate,
			string objectName,
			string objectType,
			string property,
			string propertyValues )
		{

			// Starts the test at HomePage as default user.
			HomePage homePage = this.browserManager.StartTestAtHomePage();

			// Navigate to the common view.
			ListView listing = homePage.ListView.NavigateToView( pathToNavigate );

			// Get the initial number of the objects in the view.
			int initialNumOfObjects = listing.NumberOfItems;

			// Navigate to search view.
			listing = homePage.SearchPane.FilteredQuickSearch( objectName, objectType );

			// Select the object and the filter property with empty value in the view.
			MetadataCardRightPane mdCard = listing.SelectObject( objectName );

			// Add the filter property with empty value and save the changes.
			mdCard.Properties.AddProperty( property );
			mdCard = mdCard.SaveAndDiscardOperations.Save();

			// Navigate to the common view.
			homePage.TopPane.TabButtons.HomeTabClick();
			listing = homePage.ListView.NavigateToView( pathToNavigate );

			// Check the expected items in the listing.
			Assert.AreEqual( initialNumOfObjects + 1, listing.NumberOfItems, ListViewItemsCountMismatchMessage );

			// Check the expected property values virtual folder is displayed in the listing.
			foreach( string propertyValue in propertyValues.Split( ';' ) )
			{
				Assert.True( listing.IsItemInListing( propertyValue ),
					$"Virtual folder with value '{propertyValue}' of grouping property '{property}' is not found." );
			}

			// Check the expected item in the listing.
			Assert.True( listing.IsItemInListing( objectName ),
				this.FormatItemNotFoundAssertionMessage( objectName ) );

		} // end CheckObjDisplayedInSameLevelWhenObjHavingEmptyValueForProp

		/// <summary>
		/// Testing that the object which not matches the current view condition 
		/// should be removed from the view.
		/// </summary>
		[Test]
		[Order( 7 )]
		[TestCase(
			"Filtered View>Employee",
			"Tina Smith",
			"Department",
			"Administration",
			Category = "Smoke" )]
		public void CheckObjIsRemovedFromTheViewWhenFilterConditionChangedInObj(
			string pathToNavigate,
			string objectName,
			string property,
			string propertyValue )
		{

			// Starts the test at HomePage as default user.
			HomePage homePage = this.browserManager.StartTestAtHomePage();

			// Navigate to the common view.
			ListView listing = homePage.ListView.NavigateToView( pathToNavigate );

			// Select the object in the view.
			MetadataCardRightPane mdCard = homePage.ListView.SelectObject( objectName );

			// Update the object to not match the current view condition.
			mdCard.Properties.SetPropertyValue( property, propertyValue );
			mdCard.SaveAndDiscardOperations.Save( false );

			// Check whether the object is removed from the view.
			Assert.False( listing.IsItemInListing( objectName ),
				this.FormatItemFoundAssertionMessage( objectName ) +
				" When object is modified to not match the view condition." );

		} // end CheckObjIsRemovedFromTheViewWhenFilterConditionChangedInObj		

		/// <summary>
		/// Testing that the Objects which have the empty value for the grouping condition 
		/// should be displayed in separate folder when Show on specific folder is set.
		/// </summary>
		[Test]
		[Order( 8 )]
		[TestCase(
			"Grouping:ShowOnFolder",
			"Empty Value",
			"Request for Proposal - Structural Engineering.doc",
			"Document",
			"Accepted",
			"Yes;No",
			Category = "Smoke" )]
		public void CheckObjDisplayedInMentionedFolderWhenObjHavingEmptyValueForProp(
			string pathToNavigate,
			string folderName,
			string objectName,
			string objectType,
			string property,
			string propertyValues )
		{

			// Starts the test at HomePage as default user.
			HomePage homePage = this.browserManager.StartTestAtHomePage();

			// Navigate to search view.
			ListView listing = homePage.SearchPane.FilteredQuickSearch( objectName, objectType );

			// Select the object and the filter property with empty value in the view.
			MetadataCardRightPane mdCard = listing.SelectObject( objectName );

			// Add the filter property with empty value and save the changes.
			mdCard.Properties.AddProperty( property );
			mdCard = mdCard.SaveAndDiscardOperations.Save();

			// Navigate to the common view.
			homePage.TopPane.TabButtons.HomeTabClick();
			listing = homePage.ListView.NavigateToView( pathToNavigate );

			// Check the expected number of items is listed in the  view.
			Assert.AreEqual( propertyValues.Split( ';' ).Length + 1, listing.NumberOfItems,
				ListViewItemsCountMismatchMessage );

			// Check the expected items in the listing.
			foreach( string propertyValue in propertyValues.Split( ';' ) )
			{
				Assert.True( listing.IsItemInListing( propertyValue ),
					$"Virtual folder with value '{propertyValue}' of grouping property '{property}' is not found." );
			}

			// Check the expected folder is displayed for the empty values.
			Assert.True( listing.IsItemInListing( folderName ),
				$"Folder '{folderName }' for objects having empty value in grouping property '{property}' is not found." );

			// Navigate to the specified folder.
			listing = homePage.ListView.NavigateToView( folderName );

			// Check the modified object is exists in the view.
			Assert.True( listing.IsItemInListing( objectName ),
				this.FormatItemNotFoundAssertionMessage( objectName ) );

		} // end CheckObjDisplayedInMentionedFolderWhenObjHavingEmptyValueForProp		

		/// <summary>
		/// Testing that the Older version of object which matches the filter condition 
		/// should be displayed in the common view when Show Latest Version is not enabled.
		/// </summary>
		[Test]
		[Order( 9 )]
		[TestCase(
			"AllVersion View",
			"John Stewart",
			"Employee",
			"Department",
			"Sales",
			Description = "Older version of object in the common view with no grouping level", Category = "Smoke" )]
		public void CheckCommonViewContainsOldVerOfObjBasedOnTheViewCondition(
			string pathToNavigate,
			string objectName,
			string objectType,
			string property,
			string propertyValue )
		{

			// Starts the test at HomePage as default user.
			HomePage homePage = this.browserManager.StartTestAtHomePage();

			// Navigate to the specified folder.
			ListView listing = homePage.ListView.NavigateToView( pathToNavigate );

			// Select the object in the view.
			MetadataCardRightPane mdCard = listing.SelectObject( objectName );

			// Get the grouping level.
			int groupingLevel = pathToNavigate.Split( '>' ).Length - 1;

			// Get the filter condition property value from metadatacard.
			string filterConditionValue = mdCard.Properties.GetPropertyValue( property );

			// Get the object version.
			int oldVerOfObj = mdCard.Header.Version;

			// Navigate to search view.
			listing = homePage.SearchPane.FilteredQuickSearch( objectName, objectType );

			// Select the object in the view.
			mdCard = homePage.ListView.SelectObject( objectName );

			// Update the object to not match the current view condition.
			mdCard.Properties.SetPropertyValue( property, propertyValue );
			mdCard = mdCard.SaveAndDiscardOperations.Save();

			// Navigate to home view.
			homePage.TopPane.TabButtons.HomeTabClick();

			// Navigate to the specified folder.
			listing = homePage.ListView.NavigateToView( pathToNavigate );

			// Check the modified object is exists in the view.
			Assert.True( listing.IsItemInListing( objectName ),
				this.FormatItemNotFoundAssertionMessage( objectName ) +
				" [Show latest version is not enabled.]" );

			// Select the object in the view.
			mdCard = homePage.ListView.SelectObject( objectName );

			// Get the filter condition property value from metadatacard.
			string actualValue = mdCard.Properties.GetPropertyValue( property );

			// Check older version of object.
			Assert.AreEqual( oldVerOfObj, mdCard.Header.Version,
				"Displayed object in view is not the old version." );

			// Check whether older version of object have the matching filter value.
			Assert.AreEqual( filterConditionValue, actualValue,
				ViewConditionMismatchMessage + " [Show latest version is not enabled.]" );

		} // end CheckCommonViewContainsOldVerOfObjBasedOnTheViewCondition		

		/// <summary>
		/// Testing that the user is able to navigate to the common views
		/// which have different grouping levels.
		/// </summary>
		[Test]
		[Order( 10 )]
		[TestCase(
			"readonly",
			"FirstNavigation View>Document>Meeting Notice>John Stewart>July 2007>2007>July",
			"Class",
			"Meeting Notice",
			"Invitation to Project Meeting.doc",
			Description = "Read only user", Category = "Smoke" )]
		[TestCase(
			"user",
			"SecondNavigation View>Customer>Customer>(M-Files Server)>USA",
			"Country",
			"USA",
			"Reece, Murphy and Partners",
			Description = "Normal user", Category = "Smoke" )]
		[TestCase(
			"external",
			"External View>accounting, bookkeeping",
			"Keywords",
			"accounting, bookkeeping",
			"Income Statement 10/2006.xls",
			Description = "External user", Category = "Smoke" )]
		public void NavigationInCommonViewWithDifferentGroupingLevelsAsDifferentUsers(
			string user,
			string pathToNavigate,
			string property,
			string propertyValue,
			string objectName )
		{

			// Starts the test at HomePage as specified user.
			HomePage homePage = this.browserManager.FreshLoginAndStartTestAtHomePage(
				this.mfContext.UsernameOfUser( user ), this.mfContext.PasswordOfUser( user ), this.vaultName );

			// Navigate to the path in the common view.
			ListView listing = homePage.ListView.NavigateToView( pathToNavigate );

			// Check expected item listed in the view.
			Assert.True( homePage.ListView.IsItemInListing( objectName ),
				this.FormatItemNotFoundAssertionMessage( objectName ) );

			// Select the item in the view.
			MetadataCardRightPane mdCard = listing.SelectObject( objectName );

			// Check expected property value displayed in the metadatacard for the selected object.
			Assert.AreEqual( propertyValue, mdCard.Properties.GetPropertyValue( property ),
				ViewConditionMismatchMessage );

			// Close the right pane and get the breadcrumb values.
			homePage.TopPane.RightPaneToggler.CollapseRightPane();
			var breadCrumbItems = homePage.TopPane.BreadCrumb;

			// Open the right pane.
			homePage.TopPane.RightPaneToggler.ExpandRightPane();

			// Check breadcrumb displayed the navigated path.
			Assert.AreEqual( ( this.vaultName + ">" + pathToNavigate ).Split( '>' ), breadCrumbItems,
				BreadCrumbItemMismatchMessage );

			// Close the driver.
			this.browserManager.EnsureQuitBrowser();

		} // end NavigationInCommonViewWithDifferentGroupingLevelsAsDifferentUsers	

		/// <summary>
		/// Testing that the view is updated accordingly when grouping level is removed from the view.
		/// </summary>		
		[Test]
		[Order( 11 )]
		[TestCase(
			"GroupingLevel One",
			"GroupingLevel One",
			1,
			"document",
			"East Elevation.dwg",
			Category = "Smoke" )]
		[TestCase(
			"GroupingLevel Two>Document",
			"GroupingLevel Two",
			2,
			"Meeting Notice",
			"John Stewart",
			Category = "Smoke" )]
		[TestCase(
			"GroupingLevel Three>Customer>Customer",
			"GroupingLevel Three",
			3,
			"(M-Files Server)",
			"USA",
			Category = "Smoke" )]
		public void CheckViewIsUpdatedWhenGroupingLevelIsRemoved(
			string pathToNavigate,
			string viewName,
			int groupingLevel,
			string groupingLevelItem,
			string itemName )
		{
			// Starts the test at HomePage as default user.
			HomePage homePage = this.browserManager.StartTestAtHomePage();

			// Navigate to the view.
			ListView listing = homePage.ListView.NavigateToView( pathToNavigate );

			// Check whether the grouping level is displayed.
			Assert.True( listing.IsItemInListing( groupingLevelItem ),
				this.FormatItemNotFoundAssertionMessage( groupingLevelItem ) );

			// Navigate back to the home view.
			homePage.TopPane.TabButtons.HomeTabClick();

			// Remove the grouping level.
			this.RemoveGroupingLevelInView( viewName, groupingLevel );

			// Navigate to the view.
			listing = homePage.ListView.NavigateToView( pathToNavigate );

			// Check whether the grouping level is removed.
			Assert.False( listing.IsItemInListing( groupingLevelItem ),
				this.FormatItemFoundAssertionMessage( groupingLevelItem ) );

			// Check whether the next level or object is displayed in the view.
			Assert.True( listing.IsItemInListing( itemName ),
				this.FormatItemNotFoundAssertionMessage( itemName ) );

		} // end CheckViewIsUpdatedWhenGroupingLevelIsRemoved

		[Test]
		[Category( "UI" )]
		public void VirtualFolderInAssignedToMeGridMiniView()
		{
			HomePage homePage = this.browserManager.StartTestAtHomePage();

			string assignmentName1 = "Task assignment";
			string assignmentName2 = "Approval assignment";

			string assignmentClass1 = "Assignment";
			string assignmentClass2 = "Any assignee can approve";

			// Create first assignment.
			MetadataCardPopout newMDCard = homePage.TopPane.CreateNewObject( "Assignment" );
			newMDCard.Properties.SetPropertyValue( "Class", assignmentClass1, 5 );
			newMDCard.Properties.SetPropertyValue( "Name or title", assignmentName1 );
			newMDCard.Properties.SetPropertyValue( "Assigned to", this.username );
			newMDCard.SaveAndDiscardOperations.Save( metadataCardLoadExpected: false );

			// Create another assignment but with different class than the first.
			newMDCard = homePage.TopPane.CreateNewObject( "Assignment" );
			newMDCard.Properties.SetPropertyValue( "Class", assignmentClass2, 5 );
			newMDCard.Properties.SetPropertyValue( "Name or title", assignmentName2 );
			newMDCard.Properties.SetPropertyValue( "Assigned to", this.username );
			newMDCard.SaveAndDiscardOperations.Save( metadataCardLoadExpected: false );

			// Select the first assignment in Recently accessed by me grid just to
			// make sure that the last assignment has time to create new virtual
			// folder to Assigned to me grid.
			homePage.RecentlyAccessedByMeGrid.SelectObject( assignmentName1 );

			// Assert that both assignment classes are displayed in grid as virtual folders.
			Assert.True( homePage.AssignedToMeGrid.IsItemInListing( assignmentClass1 ),
				this.FormatItemNotFoundAssertionMessage( assignmentClass1 ) );
			Assert.True( homePage.AssignedToMeGrid.IsItemInListing( assignmentClass2 ),
				this.FormatItemNotFoundAssertionMessage( assignmentClass2 ) );

			// Navigate to the first class virtual folder.
			ListView listing = homePage.AssignedToMeGrid.NavigateToView( assignmentClass1 );

			// Assert that the assignment is visible there.
			Assert.True( listing.IsItemInListing( assignmentName1 ),
				this.FormatItemNotFoundAssertionMessage( assignmentName1 ) );

			// Go back to home page.
			homePage.TopPane.TabButtons.HomeTabClick();

			// Navigate to the other class virtual folder.
			listing = homePage.AssignedToMeGrid.NavigateToView( assignmentClass2 );

			// Assert that the assignment is visible there.
			Assert.True( listing.IsItemInListing( assignmentName2 ),
				this.FormatItemNotFoundAssertionMessage( assignmentName2 ) );

			// Go back to home page.
			homePage.TopPane.TabButtons.HomeTabClick();

			// Expand/navigate to the grid.
			listing = homePage.AssignedToMeGrid.ExpandGrid();

			// Assert that both virtual folders are visible there.
			Assert.True( listing.IsItemInListing( assignmentClass1 ),
				this.FormatItemNotFoundAssertionMessage( assignmentClass1 ) );
			Assert.True( listing.IsItemInListing( assignmentClass2 ),
				this.FormatItemNotFoundAssertionMessage( assignmentClass2 ) );

			// Go to one of the virtual folders.
			listing = listing.NavigateToView( assignmentClass1 );

			// Assert that the assignment is there.
			Assert.True( listing.IsItemInListing( assignmentName1 ),
				this.FormatItemNotFoundAssertionMessage( assignmentName1 ) );
		}

	} // end Views
}