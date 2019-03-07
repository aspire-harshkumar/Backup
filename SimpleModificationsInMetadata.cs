using System;
using System.Collections.Generic;
using System.Linq;
using MFilesAPI;
using Motive.MFiles.API.Framework;
using Motive.MFiles.vNextUI.PageObjects;
using Motive.MFiles.vNextUI.PageObjects.MetadataCard;
using Motive.MFiles.vNextUI.Utilities;
using NUnit.Framework;
using NUnit.Framework.Internal;
using OpenQA.Selenium;

namespace Motive.MFiles.vNextUI.Tests
{
	[Order( -9 )]
	[Parallelizable( ParallelScope.Self )]
	class SimpleModificationsInMetadata
	{

		/// <summary>
		/// Test class identifier that is used to identify configurations for this class.
		/// </summary>
		protected virtual string classID => "SimpleModificationsInMetadata";

		private string username;
		private string password;
		private string vaultName;

		protected TestClassConfiguration configuration;

		private MFilesContext mfContext;

		protected TestClassBrowserManager browserManager;

		public SimpleModificationsInMetadata()
		{
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

			EnvironmentSetupHelper.TearDownEnvironment( this.mfContext );
		}


		[TearDown]
		public void EndTest()
		{
			this.browserManager.FinalizeBrowserStateBasedOnTestResult( TestExecutionContext.CurrentContext );
		}

		/// <summary>
		/// This method is used to update the text property value in the mentioned
		/// object metadatacard based on the parameter values.
		/// </summary>
		/// <param name="objectTypeID"></param>
		/// <param name="objectID"></param>
		/// <param name="propertyDefID"></param>
		/// <param name="propertyDefValue"></param>
		public void SetTextPropertyValueInObjectUsingMFilesAPI( int objectTypeID, int objectID, int propertyDefID, string propertyDefValue )
		{
			try
			{
				// Variable to store the vault connection.
				var vault = mfContext[ "admin" ];

				// Get the object version.
				ObjID objID = new ObjID();
				objID.SetIDs( objectTypeID, objectID );
				ObjectVersion objVersion = vault.ObjectOperations.CheckOut( objID );
				objVersion = vault.ObjectOperations.CheckIn( objVersion.ObjVer );

				// Variables to store the property value definitions.
				PropertyValues propVals = new PropertyValues();
				PropertyValue propVal = new PropertyValue();

				// Define the property value.
				propVal.PropertyDef = propertyDefID;
				propVal.TypedValue.SetValue( MFDataType.MFDatatypeText, propertyDefValue );
				propVals.Add( -1, propVal );

				vault.ObjectPropertyOperations.SetProperties( objVersion.ObjVer, propVals );
			}
			catch( Exception ex )
			{
				throw new Exception( "Exception while updating the object using M-Files API.", ex );
			}
		}

		/// <summary>
		/// This method is used to roll back the object to the specific version.
		/// </summary>
		/// <param name="objectTypeID"></param>
		/// <param name="objectID"></param>
		/// <param name="rollBackToVersion"></param>
		public void RollBackObjectToSpecificVersion( int objectTypeID, int objectID, int rollBackToVersion )
		{
			try
			{
				// Variable to store the vault connection.
				var vault = mfContext[ "admin" ];

				// Get the object version.
				ObjID objID = new ObjID();
				objID.SetIDs( objectTypeID, objectID );

				vault.ObjectOperations.Rollback( objID, rollBackToVersion );
			}
			catch( Exception ex )
			{
				throw new Exception( "Exception while rolling back the object to version '" + rollBackToVersion + "' using M-Files API.", ex );
			}
		}


		/// <summary>
		/// Modifies properties of all data types in one object. The properties already have values in them and
		/// this test modifies those values. Also, uses both the object type and value list type based lookup properties.
		/// </summary>
		[Test]
		[Category( "Smoke" )]
		[TestCase(
			"EditPropertiesOfAllTypesInOneObject.docx",
			"Keywords", "Modify Keywords",
			"Description", "Modify Description",
			"Department", "Marketing",
			"Meeting type", "Marketing meeting",
			"Owner", "John Stewart",
			"Customer", "OMCC Corporation",
			"Document date", "5/2/2018",
			"Time property", "07:15:46 AM",
			"Integer property", "9476",
			"Real number property", "53.24",
			"Accepted", "No" )]
		public void EditPropertiesOfAllTypesInOneObject(
			string objectName,
			string textProp, string textValue,
			string mlTxtProp, string mlTxtValue,
			string listSsluProp, string listSsluValue,
			string listMsluProp, string listMsluValue,
			string objSsluProp, string objSsluValue,
			string objMsluProp, string objMsluValue,
			string dateProp, string dateValue,
			string timeProp, string timeValue,
			string integerProp, string integerValue,
			string realNumberProp, string realNumberValue,
			string booleanProp, string booleanValue )
		{

			HomePage homePage = browserManager.StartTestAtHomePage();

			ListView listing = homePage.SearchPane.QuickSearch( objectName );

			MetadataCardRightPane mdCard = listing.SelectObject( objectName );

			// Get the initial object version.
			int initialObjVersion = mdCard.Header.Version;

			// Modify values of different property types.

			// Text.
			mdCard.Properties.SetPropertyValue( textProp, textValue );

			// Text multi-line.
			mdCard.Properties.SetPropertyValue( mlTxtProp, mlTxtValue );

			// Value list SSLU.
			mdCard.Properties.SetPropertyValue( listSsluProp, listSsluValue );

			// Value list MSLU.
			mdCard.Properties.SetPropertyValue( listMsluProp, listMsluValue );

			// Object type SSLU.
			mdCard.Properties.SetPropertyValue( objSsluProp, objSsluValue );

			// Object type MSLU.
			mdCard.Properties.SetPropertyValue( objMsluProp, objMsluValue );

			// Date
			mdCard.Properties.SetPropertyValue( dateProp, dateValue );

			// Time.
			mdCard.Properties.SetPropertyValue( timeProp, timeValue );

			// Integer number.
			mdCard.Properties.SetPropertyValue( integerProp, integerValue );

			// Real number.
			mdCard.Properties.SetPropertyValue( realNumberProp, realNumberValue );

			// Boolean.
			mdCard.Properties.SetPropertyValue( booleanProp, booleanValue );

			mdCard = mdCard.SaveAndDiscardOperations.Save();

			// Assert that modified property values are displayed after saving metadata card.
			Assert.AreEqual( textValue, mdCard.Properties.GetPropertyValue( textProp ) );
			Assert.AreEqual( mlTxtValue, mdCard.Properties.GetPropertyValue( mlTxtProp ) );
			Assert.AreEqual( listSsluValue, mdCard.Properties.GetPropertyValue( listSsluProp ) );
			Assert.AreEqual( listMsluValue, mdCard.Properties.GetPropertyValue( listMsluProp ) );
			Assert.AreEqual( objSsluValue, mdCard.Properties.GetPropertyValue( objSsluProp ) );
			Assert.AreEqual( objMsluValue, mdCard.Properties.GetPropertyValue( objMsluProp ) );
			Assert.AreEqual( dateValue, mdCard.Properties.GetPropertyValue( dateProp ) );
			Assert.AreEqual( timeValue, mdCard.Properties.GetPropertyValue( timeProp ) );
			Assert.AreEqual( integerValue, mdCard.Properties.GetPropertyValue( integerProp ) );
			Assert.AreEqual( realNumberValue, mdCard.Properties.GetPropertyValue( realNumberProp ) );
			Assert.AreEqual( booleanValue, mdCard.Properties.GetPropertyValue( booleanProp ) );

			// Check if object version is increased after updating the property values.
			Assert.AreEqual( initialObjVersion + 1, mdCard.Header.Version );
		}

		/// <summary>
		/// This test goes through different property data types and modifies that type of property on
		/// separate objects. Also, uses both the object type and value list type based lookup properties.
		/// </summary>
		[Test]
		[Category( "Smoke" )]
		[TestCase(
			"City", "Tampere",
			"EditSinglePropertyInOneObject textprop",
			Description = "Text" )]
		[TestCase(
			"Description", "Testing with text content åäö etc.",
			"EditSinglePropertyInOneObject multiline-text.docx",
			Description = "Multi-line text" )]
		[TestCase(
			"Department", "Production",
			"EditSinglePropertyInOneObject valueList SSLU",
			Description = "Value list SSLU" )]
		[TestCase(
			"Country", "France",
			"EditSinglePropertyInOneObject valueList MSLU",
			Description = "Value list MSLU" )]
		[TestCase(
			"Owner", "Kimberley Miller",
			"EditSinglePropertyInOneObject objtype SSLU",
			Description = "Object type SSLU" )]
		[TestCase(
			"Customer", "Reece, Murphy and Partners",
			"EditSinglePropertyInOneObject objType MSLU",
			Description = "Object type MSLU" )]
		[TestCase(
			"Document date", "12/6/2018",
			"EditSinglePropertyInOneObject date",
			Description = "Date" )]
		[TestCase(
			"Time property", "09:44:27 PM",
			"EditSinglePropertyInOneObject time.txt",
			Description = "Time" )]
		[TestCase(
			"Integer property", "45635",
			"EditSinglePropertyInOneObject integer.bmp",
			Description = "Integer number" )]
		[TestCase(
			"Real number property", "188.77",
			"EditSinglePropertyInOneObject real number.pptx",
			Description = "Real number" )]
		[TestCase(
			"Accepted", "Yes",
			"EditSinglePropertyInOneObject boolean.xlsx",
			Description = "Boolean" )]
		public void EditSinglePropertyInOneObject(
			string property, string value,
			string objectName )
		{
			HomePage homePage = browserManager.StartTestAtHomePage();

			// Search for the object and select it.
			ListView listing = homePage.SearchPane.QuickSearch( objectName );
			MetadataCardRightPane mdCard = listing.SelectObject( objectName );

			// Get the initial object version.
			int initialObjVersion = mdCard.Header.Version;

			// Edit one property value and save.
			mdCard.Properties.SetPropertyValue( property, value );
			mdCard = mdCard.SaveAndDiscardOperations.Save();

			// Assert that the change to the property is displayed after saving.
			Assert.AreEqual( value, mdCard.Properties.GetPropertyValue( property ) );

			// Check if object version is increased after updating the property values.
			Assert.AreEqual( initialObjVersion + 1, mdCard.Header.Version );
		}

		/// <summary>
		/// Adds properties of all data types to single object and sets their values.
		/// </summary>
		[Test]
		[Category( "Smoke" )]
		[TestCase(
			"AddPropertiesOfAllTypesToOneObject.docx",
			"Keywords", "Add Keywords",
			"Description", "Add Description",
			"Department", "Production",
			"Meeting type", "Board meeting",
			"Owner", "Alex Kramer",
			"Customer", "RGPP Partnership",
			"Document date", "5/25/1995",
			"Time property", "11:00:29 PM",
			"Integer property", "-14",
			"Real number property", "-76.99",
			"Accepted", "Yes" )]
		public void AddPropertiesOfAllTypesToOneObject(
			string objectName,
			string textProp, string textValue,
			string mlTxtProp, string mlTxtValue,
			string listSsluProp, string listSsluValue,
			string listMsluProp, string listMsluValue,
			string objSsluProp, string objSsluValue,
			string objMsluProp, string objMsluValue,
			string dateProp, string dateValue,
			string timeProp, string timeValue,
			string integerProp, string integerValue,
			string realNumberProp, string realNumberValue,
			string booleanProp, string booleanValue )
		{

			HomePage homePage = browserManager.StartTestAtHomePage();

			ListView listing = homePage.SearchPane.QuickSearch( objectName );

			MetadataCardRightPane mdCard = listing.SelectObject( objectName );

			// Get the initial object version.
			int initialObjVersion = mdCard.Header.Version;

			// Text.
			mdCard.Properties.AddPropertyAndSetValue( textProp, textValue );

			// Text multi-line.
			mdCard.Properties.AddPropertyAndSetValue( mlTxtProp, mlTxtValue );

			// Value list SSLU.
			mdCard.Properties.AddPropertyAndSetValue( listSsluProp, listSsluValue );

			// Value list MSLU.
			mdCard.Properties.AddPropertyAndSetValue( listMsluProp, listMsluValue );

			// Object type SSLU.
			mdCard.Properties.AddPropertyAndSetValue( objSsluProp, objSsluValue );

			// Object type MSLU.
			mdCard.Properties.AddPropertyAndSetValue( objMsluProp, objMsluValue );

			// Date
			mdCard.Properties.AddPropertyAndSetValue( dateProp, dateValue );

			// Time.
			mdCard.Properties.AddPropertyAndSetValue( timeProp, timeValue );

			// Integer number.
			mdCard.Properties.AddPropertyAndSetValue( integerProp, integerValue );

			// Real number.
			mdCard.Properties.AddPropertyAndSetValue( realNumberProp, realNumberValue );

			// Boolean.
			mdCard.Properties.AddPropertyAndSetValue( booleanProp, booleanValue );

			mdCard = mdCard.SaveAndDiscardOperations.Save();

			// Assert that modified property values are displayed after saving metadata card.
			Assert.AreEqual( textValue, mdCard.Properties.GetPropertyValue( textProp ) );
			Assert.AreEqual( mlTxtValue, mdCard.Properties.GetPropertyValue( mlTxtProp ) );
			Assert.AreEqual( listSsluValue, mdCard.Properties.GetPropertyValue( listSsluProp ) );
			Assert.AreEqual( listMsluValue, mdCard.Properties.GetPropertyValue( listMsluProp ) );
			Assert.AreEqual( objSsluValue, mdCard.Properties.GetPropertyValue( objSsluProp ) );
			Assert.AreEqual( objMsluValue, mdCard.Properties.GetPropertyValue( objMsluProp ) );
			Assert.AreEqual( dateValue, mdCard.Properties.GetPropertyValue( dateProp ) );
			Assert.AreEqual( timeValue, mdCard.Properties.GetPropertyValue( timeProp ) );
			Assert.AreEqual( integerValue, mdCard.Properties.GetPropertyValue( integerProp ) );
			Assert.AreEqual( realNumberValue, mdCard.Properties.GetPropertyValue( realNumberProp ) );
			Assert.AreEqual( booleanValue, mdCard.Properties.GetPropertyValue( booleanProp ) );

			// Check if object version is increased after adding different properties with values in the metadatacard.
			Assert.AreEqual( initialObjVersion + 1, mdCard.Header.Version );
		}

		/// <summary>
		/// Adds properties of different data types to single object but leaves their values empty.
		/// </summary>
		[Test]
		[Category( "Smoke" )]
		[TestCase(
			"AddEmptyPropertiesOfAllTypesToOneObject.pptx",
			"Keywords", "Description",
			"Department", "Meeting type",
			"Owner", "Customer",
			"Document date", "Time property",
			"Integer property", "Real number property",
			"Accepted" )]
		public void AddEmptyPropertiesOfAllTypesToOneObject(
			string objectName,
			string textProp, string multilineProp,
			string valueListSsluProp, string valueListMsluProp,
			string objTypeSsluProp, string objTypeMsluProp,
			string dateProp, string timeProp,
			string integerProp, string realNumberProp,
			string booleanProp )
		{
			HomePage homePage = browserManager.StartTestAtHomePage();

			ListView listing = homePage.SearchPane.QuickSearch( objectName );

			MetadataCardRightPane mdCard = listing.SelectObject( objectName );

			// Get the initial object version.
			int initialObjVersion = mdCard.Header.Version;

			// Text.
			mdCard.Properties.AddProperty( textProp );

			// Text multi-line.
			mdCard.Properties.AddProperty( multilineProp );

			// Value list SSLU.
			mdCard.Properties.AddProperty( valueListSsluProp );

			// Value list MSLU.
			mdCard.Properties.AddProperty( valueListMsluProp );

			// Object type SSLU.
			mdCard.Properties.AddProperty( objTypeSsluProp );

			// Object type MSLU.
			mdCard.Properties.AddProperty( objTypeMsluProp );

			// Date
			mdCard.Properties.AddProperty( dateProp );

			// Time.
			mdCard.Properties.AddProperty( timeProp );

			// Integer number.
			mdCard.Properties.AddProperty( integerProp );

			// Real number.
			mdCard.Properties.AddProperty( realNumberProp );

			// Boolean.
			mdCard.Properties.AddProperty( booleanProp );

			mdCard = mdCard.SaveAndDiscardOperations.Save();

			string expectedEmptyPropertyValue = "";

			// Assert that the added properties are displayed with empty values after saving metadata card.
			Assert.AreEqual( expectedEmptyPropertyValue, mdCard.Properties.GetPropertyValue( textProp ) );
			Assert.AreEqual( expectedEmptyPropertyValue, mdCard.Properties.GetPropertyValue( multilineProp ) );
			Assert.AreEqual( expectedEmptyPropertyValue, mdCard.Properties.GetPropertyValue( valueListSsluProp ) );
			Assert.AreEqual( expectedEmptyPropertyValue, mdCard.Properties.GetPropertyValue( valueListMsluProp ) );
			Assert.AreEqual( expectedEmptyPropertyValue, mdCard.Properties.GetPropertyValue( objTypeSsluProp ) );
			Assert.AreEqual( expectedEmptyPropertyValue, mdCard.Properties.GetPropertyValue( objTypeMsluProp ) );
			Assert.AreEqual( expectedEmptyPropertyValue, mdCard.Properties.GetPropertyValue( dateProp ) );
			Assert.AreEqual( expectedEmptyPropertyValue, mdCard.Properties.GetPropertyValue( timeProp ) );
			Assert.AreEqual( expectedEmptyPropertyValue, mdCard.Properties.GetPropertyValue( integerProp ) );
			Assert.AreEqual( expectedEmptyPropertyValue, mdCard.Properties.GetPropertyValue( realNumberProp ) );
			Assert.AreEqual( expectedEmptyPropertyValue, mdCard.Properties.GetPropertyValue( booleanProp ) );

			// Check if object version is increased after adding different properties without values in the metadatacard.
			Assert.AreEqual( initialObjVersion + 1, mdCard.Header.Version );
		}


		/// <summary>
		/// Removes properties of all data types from single object.
		/// </summary>
		[Test]
		[Category( "Smoke" )]
		[TestCase(
			"RemovePropertiesOfAllTypesFromOneObject.docx",
			"Keywords", "Description",
			"Department", "Meeting type",
			"Owner", "Customer",
			"Document date", "Time property",
			"Integer property", "Real number property",
			"Accepted" )]
		public void RemovePropertiesOfAllTypesFromOneObject(
			string objectName,
			string textProp, string multilineProp,
			string valueListSsluProp, string valueListMsluProp,
			string objTypeSsluProp, string objTypeMsluProp,
			string dateProp, string timeProp,
			string integerProp, string realNumberProp,
			string booleanProp )
		{
			HomePage homePage = browserManager.StartTestAtHomePage();

			ListView listing = homePage.SearchPane.QuickSearch( objectName );

			MetadataCardRightPane mdCard = listing.SelectObject( objectName );

			// Get the initial object version.
			int initialObjVersion = mdCard.Header.Version;

			// Text.
			mdCard.Properties.RemoveProperty( textProp );

			// Text multi-line.
			mdCard.Properties.RemoveProperty( multilineProp );

			// Value list SSLU.
			mdCard.Properties.RemoveProperty( valueListSsluProp );

			// Value list MSLU.
			mdCard.Properties.RemoveProperty( valueListMsluProp );

			// Object type SSLU.
			mdCard.Properties.RemoveProperty( objTypeSsluProp );

			// Object type MSLU.
			mdCard.Properties.RemoveProperty( objTypeMsluProp );

			// Date
			mdCard.Properties.RemoveProperty( dateProp );

			// Time.
			mdCard.Properties.RemoveProperty( timeProp );

			// Integer number.
			mdCard.Properties.RemoveProperty( integerProp );

			// Real number.
			mdCard.Properties.RemoveProperty( realNumberProp );

			// Boolean.
			mdCard.Properties.RemoveProperty( booleanProp );

			mdCard = mdCard.SaveAndDiscardOperations.Save();

			// Assert that all removed properties are not displayed after saving.
			Assert.False( mdCard.Properties.IsPropertyInMetadataCard( textProp ) );
			Assert.False( mdCard.Properties.IsPropertyInMetadataCard( multilineProp ) );
			Assert.False( mdCard.Properties.IsPropertyInMetadataCard( valueListSsluProp ) );
			Assert.False( mdCard.Properties.IsPropertyInMetadataCard( valueListMsluProp ) );
			Assert.False( mdCard.Properties.IsPropertyInMetadataCard( objTypeSsluProp ) );
			Assert.False( mdCard.Properties.IsPropertyInMetadataCard( objTypeMsluProp ) );
			Assert.False( mdCard.Properties.IsPropertyInMetadataCard( dateProp ) );
			Assert.False( mdCard.Properties.IsPropertyInMetadataCard( timeProp ) );
			Assert.False( mdCard.Properties.IsPropertyInMetadataCard( integerProp ) );
			Assert.False( mdCard.Properties.IsPropertyInMetadataCard( realNumberProp ) );
			Assert.False( mdCard.Properties.IsPropertyInMetadataCard( booleanProp ) );

			// Check if object version is increased after adding different properties without values in the metadatacard.
			Assert.AreEqual( initialObjVersion + 1, mdCard.Header.Version );
		}

		/// <summary>
		/// Removes a property from metadata card.
		/// </summary>
		[Test]
		[Category( "Smoke" )]
		[TestCase( "RemoveSinglePropertyFromOneObject text.xlsx", "Keywords" )]
		public void RemoveSinglePropertyFromOneObject( string objectName, string property )
		{
			HomePage homePage = browserManager.StartTestAtHomePage();

			ListView listing = homePage.SearchPane.QuickSearch( objectName );

			MetadataCardRightPane mdCard = listing.SelectObject( objectName );

			mdCard.Properties.RemoveProperty( property );

			mdCard = mdCard.SaveAndDiscardOperations.Save();

			// Assert that the property is not in metadata card after removing it and saving changes.
			Assert.False( mdCard.Properties.IsPropertyInMetadataCard( property ) );
		}


		/// <summary>
		/// Sets multiple values to multi-select lookup property.
		/// </summary>
		[Test]
		[Category( "Smoke" )]
		[TestCase(
			"Lily.jpg",
			"Customer", "CBH International",
			"Fortney Nolte Associates", "RGPP Partnership" )]
		public void AddMultipleValuesToMultiSelectLookup(
			string objectName,
			string msluProperty, string msluValue0,
			string msluValue1, string msluValue2 )
		{
			HomePage homePage = browserManager.StartTestAtHomePage();

			ListView listing = homePage.SearchPane.QuickSearch( objectName );

			MetadataCardRightPane mdCard = listing.SelectObject( objectName );

			// Enter 3 values to MSLU property.
			mdCard.Properties.SetPropertyValue( msluProperty, msluValue0 );
			mdCard.Properties.SetMultiSelectLookupPropertyValueByIndex( msluProperty, msluValue1, 1 );
			mdCard.Properties.SetMultiSelectLookupPropertyValueByIndex( msluProperty, msluValue2, 2 );

			mdCard.SaveAndDiscardOperations.Save();

			// Assert that all entered MSLU values are displayed after saving changes.
			Assert.AreEqual( msluValue0, mdCard.Properties.GetPropertyValue( msluProperty ) );
			Assert.AreEqual( msluValue1, mdCard.Properties.GetMultiSelectLookupPropertyValueByIndex( msluProperty, 1 ) );
			Assert.AreEqual( msluValue2, mdCard.Properties.GetMultiSelectLookupPropertyValueByIndex( msluProperty, 2 ) );
		}


		/// <summary>
		/// Discards changes after modifying a property value and adding a property and setting its value.
		/// </summary>
		[Test]
		[Category( "Smoke" )]
		[TestCase(
			"Invitation to Project Meeting 2/2007.doc",
			"Project", "Central Plains Area Development",
			"Department", "Production" )]
		public void DiscardMetadataChanges(
			string objectName,
			string modifyProperty, string modifyPropertyValue,
			string addProperty, string addPropertyValue )
		{

			HomePage homePage = browserManager.StartTestAtHomePage();

			ListView listing = homePage.SearchPane.QuickSearch( objectName );

			MetadataCardRightPane mdCard = listing.SelectObject( objectName );

			// Get the original property value before modification.
			string originalPropValue = mdCard.Properties.GetPropertyValue( modifyProperty );

			// Modify the property value.
			mdCard.Properties.SetPropertyValue( modifyProperty, modifyPropertyValue );

			// Add a new property and set its value.
			mdCard.Properties.AddPropertyAndSetValue( addProperty, addPropertyValue );

			// Discard changes.
			mdCard = mdCard.DiscardChanges();

			// Assert that the modified property value has its original value after discard.
			Assert.AreEqual( originalPropValue, mdCard.Properties.GetPropertyValue( modifyProperty ) );

			// Assert that the added property is no longer in metadata card after discard changes.
			Assert.False( mdCard.Properties.IsPropertyInMetadataCard( addProperty ) );
		}

		/// <summary>
		/// Modifies a property value and adds a property and sets its value to right pane metadata card. Then
		/// pops out the metadata card and the changes transfer to the popped out metadata card. Finally saves
		/// the changes in the popped out metadata card and the changes are again visible in the right pane
		/// metadata card.
		/// </summary>
		[Test]
		[Category( "Smoke" )]
		[TestCase(
			"Project Meeting Minutes 1/2006.txt",
			"Document date", "6/7/2006",
			"Owner", "Andy Nash" )]
		public void MetadataChangesFromRightPaneToPoppedOutMetadataCard(
			string objectName,
			string modifyProperty, string modifyPropertyValue,
			string addProperty, string addPropertyValue )
		{
			HomePage homePage = browserManager.StartTestAtHomePage();

			ListView listing = homePage.SearchPane.QuickSearch( objectName );

			MetadataCardRightPane mdCard = listing.SelectObject( objectName );

			// Modify a property and add another property and set its value.
			mdCard.Properties.SetPropertyValue( modifyProperty, modifyPropertyValue );
			mdCard.Properties.AddPropertyAndSetValue( addProperty, addPropertyValue );

			// Popout the metadata card and the changes from right pane should transfer 
			// to the popped out metadata card.
			MetadataCardPopout popoutMDCard = mdCard.PopoutMetadataCard();

			// Assert that the modified property is displayed in the popped out metadata card.
			Assert.AreEqual( modifyPropertyValue, popoutMDCard.Properties.GetPropertyValue( modifyProperty ) );

			// Assert also that the added property is visible in the popped out metadata card.
			Assert.AreEqual( addPropertyValue, popoutMDCard.Properties.GetPropertyValue( addProperty ) );

			// Save changes in popout metadata card.
			mdCard = popoutMDCard.SaveAndDiscardOperations.Save();

			// Assert that the same modifications are displayed in right pane metadata card after saving the 
			// popped out metadata card.
			Assert.AreEqual( modifyPropertyValue, mdCard.Properties.GetPropertyValue( modifyProperty ) );
			Assert.AreEqual( addPropertyValue, mdCard.Properties.GetPropertyValue( addProperty ) );
		}

		[Test]
		[Category( "MetadataCard" )]
		[TestCase(
			"Scott Butler",
			"Contact person",
			"E-mail address",
			"test@m-files.com",
			149,
			34,
			1060 )]
		public void MetadataCardUpdateAfterObjUpdatedUsingMFilesAPI(
			string objectName,
			string objectType,
			string propertyName,
			string propertyValue,
			int objTypeID,
			int objID,
			int propertyID )
		{

			// Start the test at home page.
			HomePage homePage = browserManager.StartTestAtHomePage();

			// Search for the object.
			ListView listing = homePage.SearchPane.FilteredQuickSearch( objectName, objectType );

			// Select the object in list view.
			MetadataCardRightPane mdCard = listing.SelectObject( objectName );

			// Get the initial object version.
			int initialObjVersion = mdCard.Header.Version;

			// Navigate back to the home view.
			homePage.TopPane.TabButtons.HomeTabClick();

			// Update the object using M-Files API.
			this.SetTextPropertyValueInObjectUsingMFilesAPI( objTypeID, objID, propertyID, propertyValue );

			// Search for the same object.
			listing = homePage.SearchPane.FilteredQuickSearch( objectName, objectType );

			// Select the object in list view.
			mdCard = listing.SelectObject( objectName );

			// Assert that object version is increased for the selected object.
			Assert.AreEqual( initialObjVersion + 1, mdCard.Header.Version, "Mismatch between the expected and actual object version." );

			// Assert that property is updated with the expected value.
			Assert.AreEqual( propertyValue, mdCard.Properties.GetPropertyValue( propertyName ), "Mismatch between the expected and actual property('" + propertyName + "') value." );

		}

		/// <summary>
		/// Testing that metadatacard is retained with the edited value when switching between the tabs.
		/// </summary>
		[Test]
		[Category( "MetadataCard" )]
		[TestCase(
			"Preview document.docx",
			"Document",
			"Keywords",
			"Sample text edit",
			PreviewPane.PreviewStatus.ContentDisplayed,
			"Scope;Object type" )]
		[TestCase(
			"Redesign of ESTT Marketing Material",
			"Project",
			"Project manager",
			"Mike Taylor",
			PreviewPane.PreviewStatus.NothingToPreview,
			"Scope;Object type" )]
		public void ToggleBetweenTabsAfterMetadataEdit(
			string objectName,
			string objectType,
			string property,
			string propertyValue,
			PreviewPane.PreviewStatus expectedPreviewStatus,
			string facetHeaders )
		{
			// Get the expected search facet headers.
			List<string> expectedFacetHeaders = facetHeaders.Split( ';' ).ToList();

			// Start the test at home page.
			HomePage homePage = browserManager.StartTestAtHomePage();

			// Search for the object.
			ListView listing = homePage.SearchPane.FilteredQuickSearch( objectName, objectType );

			// Select the object in list view.
			MetadataCardRightPane mdCard = listing.SelectObject( objectName );

			// Edit the property value in the metadatacard.
			mdCard.Properties.SetPropertyValue( property, propertyValue );

			// Navigate to the preview tab.
			PreviewPane preview = homePage.TopPane.TabButtons.PreviewTabClick();

			// Assert that preview is displayed.
			Assert.AreEqual( expectedPreviewStatus, preview.Status, "Mismatch between the expected and actual preview status." );

			// Navigate to the filter tab.
			SearchFilters filters = homePage.TopPane.TabButtons.SearchFiltersTabClick();

			// Assert that expected facet headers displayed in the filters tab.
			Assert.AreEqual( expectedFacetHeaders, filters.FilterFacetHeaders, "Mismatch between the expected and actual search filter facet headers." );

			// Navigate back to the metadatacard tab.
			mdCard = homePage.TopPane.TabButtons.MetadataTabClick( objectName );

			// Assert that edited property value is retained in the metadatacard.
			Assert.AreEqual( propertyValue, mdCard.Properties.GetPropertyValue( property ), "Mismatch between the expected and actual property value." );

			// Discard the changes.
			mdCard.DiscardChanges();

		}

		/// <summary>
		/// Testing that metadatacard header is expanded and collapsed and settings retained
		/// when navigating between different objects.
		/// </summary>		
		[Test]
		[Category( "MetadataCard" )]
		[TestCase(
			"2. Manage Customers",
			"CBH International",
			"OMCC Corporation" )]
		public virtual void CollapseAndExpandMetadataCardHeader( string viewToNavigate, string object1Name, string object2Name )
		{
			// Additional assertion message variable declaration.
			string additionalAssertMessage = "Mismatch between the expected and actual metadatacard header state.";

			// Start the test at home page.
			HomePage homePage = browserManager.StartTestAtHomePage();

			// Search for the object.
			ListView listing = homePage.ListView.NavigateToView( viewToNavigate );

			// Select the object in list view.
			MetadataCardRightPane mdCard = listing.SelectObject( object1Name );

			// Assert that metadatacard in expanded state.
			Assert.AreEqual( MetadataCardHeaderStatus.Expanded, mdCard.HeaderOptionRibbon.HeaderStatus,
				additionalAssertMessage );

			// Collapse the metadatacard header.
			mdCard.HeaderOptionRibbon.CollapseHeader();

			// Assert that metadatacard in collapsed state.
			Assert.AreEqual( MetadataCardHeaderStatus.Collapsed, mdCard.HeaderOptionRibbon.HeaderStatus,
				additionalAssertMessage );

			// Select another object in the view.
			mdCard = listing.SelectObject( object2Name );

			// Assert that metadatacard in collapsed state.
			Assert.AreEqual( MetadataCardHeaderStatus.Collapsed, mdCard.HeaderOptionRibbon.HeaderStatus,
				additionalAssertMessage );

			// Expand the metadatacard header.
			mdCard.HeaderOptionRibbon.ExpandHeader();

			// Assert that metadatacard in expanded state.
			Assert.AreEqual( MetadataCardHeaderStatus.Expanded, mdCard.HeaderOptionRibbon.HeaderStatus,
				additionalAssertMessage );

			// Select another object in the view.
			mdCard = listing.SelectObject( object1Name );

			// Assert that metadatacard in expanded state.
			Assert.AreEqual( MetadataCardHeaderStatus.Expanded, mdCard.HeaderOptionRibbon.HeaderStatus,
				additionalAssertMessage );
		}

		[Test]
		[Category( "MetadataCard" )]
		[TestCase(
			"Invoice A113-112-33 - GNT.pdf",
			"Document",
			"Assignment description",
			"Test content.",
			0,
			364 )]
		public void MetadataCardUpdateAfterObjVersionRollBackUsingMFilesAPI(
			string objectName,
			string objectType,
			string propertyName,
			string propertyValue,
			int objTypeID,
			int objID )
		{

			// Start the test at home page.
			HomePage homePage = browserManager.StartTestAtHomePage();

			// Search for the object.
			ListView listing = homePage.SearchPane.FilteredQuickSearch( objectName, objectType );

			// Select the object in list view.
			MetadataCardRightPane mdCard = listing.SelectObject( objectName );

			// Get the initial object version and some property value.
			int initialObjVersion = mdCard.Header.Version;
			string initialPropertyValue = mdCard.Properties.GetPropertyValue( propertyName );

			// Update the property value and save the changes.
			mdCard.Properties.SetPropertyValue( propertyName, propertyValue );

			// Save the changes.
			mdCard = mdCard.SaveAndDiscardOperations.Save();

			// Navigate back to the home view.
			homePage.TopPane.TabButtons.HomeTabClick();

			// Update the object using M-Files API.
			this.RollBackObjectToSpecificVersion( objTypeID, objID, initialObjVersion );

			// Search for the same object.
			listing = homePage.SearchPane.FilteredQuickSearch( objectName, objectType );

			// Select the object in list view.
			mdCard = listing.SelectObject( objectName );

			// Assert that object version is increased for the selected object.
			Assert.AreEqual( initialObjVersion + 2, mdCard.Header.Version, "Mismatch between the expected and actual object version after roll back." );

			// Assert that property is updated with the expected value.
			Assert.AreEqual( initialPropertyValue, mdCard.Properties.GetPropertyValue( propertyName ), "Mismatch between the expected and actual property('" + propertyName + "') value after roll back to previous version of object." );

		}

		[Test]
		[TestCase(
			"South Elevation.dwg",
			"Document",
			"Customer",
			"CBH International",
			"Project",
			Description = "Filtering property value which doesn't have any matching values in filtered property." )]
		[TestCase(
			"Requirements Specification / Legal Records Management.doc",
			"Document",
			"Customer",
			"A&A Consulting (AEC)",
			"Project",
			Description = "Filtering property value which has some matching values in filtered property." )]
		public void FilteredLookupPropertyValueAutoClear(
			string objectName,
			string objectType,
			string filterProperty,
			string modifyValue,
			string filteredProperty )
		{
			// Start the test and search for an object.
			HomePage homePage = this.browserManager.StartTestAtHomePage();
			ListView listing = homePage.SearchPane.FilteredQuickSearch( objectName, objectType );

			// Select the object.
			MetadataCardRightPane mdCard = listing.SelectObject( objectName );

			// Change value of filter property.
			mdCard.Properties.SetPropertyValue( filterProperty, modifyValue );

			// Assert that the filtered property should be cleared because it doesn't match
			// the filter anymore.
			Assert.AreEqual( "", mdCard.Properties.GetPropertyValue( filteredProperty ),
				$"Property '{filteredProperty}' was not automatically cleared when it no longer matches the filter." );

			// Save changes.
			mdCard.SaveAndDiscardOperations.Save();
		}

		[Test]
		[TestCase(
			"Parrot.jpg",
			"Document",
			"Name or title",
			"ÄÅÖöäå!#¤%&/()=?Bird",
			".jpg",
			Description = "Document object.")]
		[TestCase(
			"Warwick Systems & Technology",
			"Customer",
			"Customer name",
			"系统和技术",
			"",
			Description = "Non-document object." )]
		public void RenameObject( 
			string objectName,
			string objectType,
			string nameProperty,
			string newTitle,
			string fileExtension )
		{
			HomePage homePage = this.browserManager.StartTestAtHomePage();

			// Search for the object.
			ListView listing = homePage.SearchPane.FilteredQuickSearch( objectName, objectType );

			// Modify object name and save.
			MetadataCardRightPane mdCard = listing.SelectObject( objectName );
			mdCard.Properties.SetPropertyValue( nameProperty, newTitle );
			mdCard.SaveAndDiscardOperations.Save();

			// New object name has the new name + file extension.
			string newNameInListing = newTitle + fileExtension;

			// Assert that the new name is shown in metadata card.
			Assert.AreEqual( newTitle, mdCard.Properties.GetPropertyValue( nameProperty ),
				 "New object name is not not shown in metadata card after rename." );

			// Assert that the new name is shown in listing.
			Assert.AreEqual( newNameInListing, listing.SelectedItemName,
				"New object name is not not shown in listing after rename." );
		}
	}
}