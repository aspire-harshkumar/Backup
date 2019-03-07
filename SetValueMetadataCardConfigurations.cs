using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Motive.MFiles.API.Framework;
using Motive.MFiles.vNextUI.PageObjects;
using Motive.MFiles.vNextUI.PageObjects.MetadataCard;
using Motive.MFiles.vNextUI.Utilities;
using Motive.MFiles.vNextUI.Utilities.GeneralHelpers;
using NUnit.Framework;
using NUnit.Framework.Internal;

namespace Motive.MFiles.vNextUI.Tests
{
	[Order( -13 )]
	[Parallelizable( ParallelScope.Self )]
	class SetValueMetadataCardConfigurations
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

		public SetValueMetadataCardConfigurations()
		{
			this.classID = "SetValueMetadataCardConfigurations";
		}

		/// <summary>
		/// Asserts that expected value is visible in the property. Provides error message
		/// if assertion fails.
		/// </summary>
		/// <param name="expectedValue">Expected property value.</param>
		/// <param name="setValueProperty">Property name.</param>
		/// <param name="properties">Properties page object.</param>
		private void AssertSetValueProperty(
			string expectedValue,
			string setValueProperty,
			PropertiesInMetadataCard properties )
		{
			Assert.AreEqual( expectedValue, properties.GetPropertyValue( setValueProperty ),
				$"Unexpected value in set value property '{setValueProperty}'." );
		}

		/// <summary>
		/// Asserts that expected values are visible in the MSLU property. Provides error message
		/// if assertion fails.
		/// </summary>
		/// <param name="expectedMsluValues">Expected MSLU values as a list of strings.</param>
		/// <param name="setValueMsluProperty">Property name.</param>
		/// <param name="properties">Properties page object.</param>
		private void AssertSetValueMsluProperty(
			List<string> expectedMsluValues,
			string setValueMsluProperty,
			PropertiesInMetadataCard properties )
		{
			Assert.AreEqual( expectedMsluValues, properties.GetMultiSelectLookupPropertyValues( setValueMsluProperty ),
				$"Mismatch between expected and actual values in set value MSLU property '{setValueMsluProperty}'." );
		}

		/// <summary>
		/// Asserts existence of property with certain label in the metadata card. Parameter can be used to
		/// define if property is expected to be found or expected to not be found in the metadata card.
		/// Provides error message if assertion fails.
		/// </summary>
		/// <param name="expectedLabel">Property label.</param>
		/// <param name="isExpectedToBeFound">Is the property expected to be found or expected to be not found.</param>
		/// <param name="properties">Properties page object.</param>
		private void AssertPropertyLabelExistenceInMetadataCard(
			string expectedLabel,
			bool isExpectedToBeFound,
			PropertiesInMetadataCard properties )
		{
			// Check if the property with the provided label is expected to be found or not.
			if( isExpectedToBeFound )
			{
				// Property should be found in the metadata card.
				Assert.True( properties.IsPropertyInMetadataCard( expectedLabel ),
					$"Property with label '{expectedLabel}' was not found in metadata card." );
			}
			else
			{
				// Property should not be found in the metadata card.
				Assert.False( properties.IsPropertyInMetadataCard( expectedLabel ),
					$"Property with label '{expectedLabel}' was found in metadata card when it was not supposed to be there." );
			}
		}

		[OneTimeSetUp]
		public void SetupTestClass()
		{
			// Initialize configurations for the test class based on test context parameters.
			this.configuration = new TestClassConfiguration( this.classID, TestContext.Parameters );

			// Define users required by this test class.
			UserProperties[] users = EnvironmentSetupHelper.GetBasicTestUsers();

			// TODO: Some environment details should probably come from configuration. For example the backend.
			this.mfContext = EnvironmentSetupHelper.SetupEnvironment( EnvironmentHelper.VaultBackend.Firebird, "Set Value Configurations.mfb", users );

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
		public void IsForcedTruePlaceholderNewObject()
		{

			string configFileName = $"{classID}\\IsForcedTruePlaceholderNewObject.json";

			EnvironmentSetupHelper.SetupMetadataCardConfiguration( mfContext, configFileName );

			HomePage homePage = this.browserManager.StartTestAtHomePage();

			// Start creating object.
			MetadataCardPopout newMDCard = homePage.TopPane.CreateNewObject( "Customer" );

			// Modifying this property may trigger configuration rule condition to set value to another property.
			string triggerPropertyCountry = "Country";

			// The value of this property will be set by configuration rule.
			string setValuePropertyCity = "City";

			// These are the placeholder values that will be inserted to the setValueProperty.
			// Note that "USA" and "Canada" also happen to be values that will trigger the rule.
			List<string> placeholders = new List<string> { "USA", "Canada", "France" };

			// These values that are expected to be set. The country name comes from placeholder.
			List<string> expectedSetValues = new List<string>
			{
				// Rule triggered when setting USA.
				"Famous city of USA is Florida",

				// Rule triggered when setting Canada.
				"Famous city of Canada is Toronto",

				// Still using the previously triggered Canada rule, but placeholder value is now France.
				"Famous city of France is Toronto"
			};

			// Go through the different placeholder values.
			for( int i = 0; i < placeholders.Count; ++i )
			{
				// Set property value to triggering property. Note that the placeholder will still affect
				// the property value even when the rule condition is not anymore fulfilled.
				newMDCard.Properties.SetPropertyValue( triggerPropertyCountry, placeholders[ i ] );

				// Assert that the expected set value is displayed.
				this.AssertSetValueProperty( expectedSetValues[ i ], setValuePropertyCity, newMDCard.Properties );
			}

			// Clear the value from the SetValue property. This causes the property to not anymore follow 
			// placeholder unless the rule is triggered again.
			newMDCard.Properties.SetPropertyValue( setValuePropertyCity, "" );

			// Set value that will not trigger the rule.
			newMDCard.Properties.SetPropertyValue( triggerPropertyCountry, "Germany" );

			// Assert that the SetValue property remains empty because the rule is not triggered.
			this.AssertSetValueProperty( "", setValuePropertyCity, newMDCard.Properties );

			// This is trigger property for another metadata configuration rule.
			string triggerPropertyDepartment = "Department";

			// Set value to trigger configuration rule.
			newMDCard.Properties.AddPropertyAndSetValue( triggerPropertyDepartment, "Sales" );

			// This property will get a set value. Also, the value of the triggering
			// "Department" property happens to be a placeholder in this property.
			string setValuePropertyPhoneNumber = "Telephone number";

			// This property will get a set value. The value is a placeholder from "Telephone number" property.
			string setValuePropertyKeywords = "Keywords";

			// Assert that both properties will have same value. "Telephone number" will be updated with placeholder value 
			// from "Department" property and "Keywords" property will be updated as the value of "Telephone number" property.
			this.AssertSetValueProperty( "Number of Sales is 456", setValuePropertyPhoneNumber, newMDCard.Properties );
			this.AssertSetValueProperty( "Number of Sales is 456", setValuePropertyKeywords, newMDCard.Properties );

			// Empty the value of "Telephone number" and assert that the "Keywords" will follow that value.
			newMDCard.Properties.SetPropertyValue( setValuePropertyPhoneNumber, "" );
			this.AssertSetValueProperty( "", setValuePropertyKeywords, newMDCard.Properties );

			// Now again back to the first rule. Set value to Canada and assert that the rule is triggered again.
			newMDCard.Properties.SetPropertyValue( triggerPropertyCountry, "Canada" );
			this.AssertSetValueProperty( "Famous city of Canada is Toronto", setValuePropertyCity, newMDCard.Properties );

			// Now again back to the second rule set. Manually set value to "Keywords". This causes that its
			// placeholder is no longer followed unless the rule is triggered again.
			string setManualValue = "Manually set value should remain.";
			newMDCard.Properties.SetPropertyValue( setValuePropertyKeywords, setManualValue );

			// Manually set value of "Telephone number". Assert that this value should now not be set to the "Keywords" because
			// the value was already manually modified.
			newMDCard.Properties.SetPropertyValue( setValuePropertyPhoneNumber, "This modification will not be set to other property." );
			this.AssertSetValueProperty( setManualValue, setValuePropertyKeywords, newMDCard.Properties );

			// Now again triggering the first rule and also the second rule.
			newMDCard.Properties.SetPropertyValue( triggerPropertyCountry, "USA" );
			newMDCard.Properties.SetPropertyValue( triggerPropertyDepartment, "Marketing" );

			// Assert that both rules should now activate.

			// Triggered by USA -> using Country as placeholder.
			this.AssertSetValueProperty( "Famous city of USA is Florida", setValuePropertyCity, newMDCard.Properties );

			// Triggered by Marketing -> using Department as placeholder.
			this.AssertSetValueProperty( "Number of Marketing is 123", setValuePropertyPhoneNumber, newMDCard.Properties );

			// Triggered by Marketing -> using Telephone number as placeholder.
			this.AssertSetValueProperty( "Number of Marketing is 123", setValuePropertyKeywords, newMDCard.Properties );

			// Clear all set values, set name, and create the object.
			newMDCard.Properties.SetPropertyValue( setValuePropertyCity, "" );
			newMDCard.Properties.SetPropertyValue( setValuePropertyPhoneNumber, "" );
			newMDCard.Properties.SetPropertyValue( setValuePropertyKeywords, "" );
			newMDCard.Properties.SetPropertyValue( "Customer name", "IsForcedTrue new test" );
			newMDCard.SaveAndDiscardOperations.Save( metadataCardLoadExpected: false );
		}

		[Test]
		public void IsForcedTruePlaceholderExistingObject()
		{

			string configFileName = $"{classID}\\IsForcedTruePlaceholderExistingObject.json";

			EnvironmentSetupHelper.SetupMetadataCardConfiguration( mfContext, configFileName );

			HomePage homePage = this.browserManager.StartTestAtHomePage();

			string objectType = "Customer";
			string objectName = "IsForcedTrue existing test";

			// Go to search view.
			ListView listing = homePage.SearchPane.FilteredQuickSearch( objectName, objectType );

			// Start creating a new object. This object creation is basically only a setup step
			// so not much verification is done during object creation.
			MetadataCardPopout newMDCard = homePage.TopPane.CreateNewObject( objectType );

			// Define properties which are essential in this test case.

			// These properties will trigger configuration rules.
			string triggerPropertyCountry = "Country";
			string triggerPropertyDepartment = "Department";

			// These properties will have set values by configuration rules.
			string setValuePropertyCity = "City";
			string setValuePropertyPhoneNumber = "Telephone number";
			string setValuePropertyKeywords = "Keywords";

			// Put all these properties to list so later all values can be checked.
			List<string> essentialProperties = new List<string>
			{
				triggerPropertyCountry, triggerPropertyDepartment,
				setValuePropertyCity, setValuePropertyPhoneNumber, setValuePropertyKeywords
			};

			// Add a couple of missing properties.
			newMDCard.Properties.AddProperty( setValuePropertyKeywords );
			newMDCard.Properties.AddProperty( triggerPropertyDepartment );

			// Trigger both rules.
			newMDCard.Properties.SetPropertyValue( triggerPropertyCountry, "USA" );
			newMDCard.Properties.SetPropertyValue( triggerPropertyDepartment, "Marketing" );

			// Modify property value of a property that had its value set.
			newMDCard.Properties.SetPropertyValue( setValuePropertyCity, "Modify value" );

			// Set object name and create the object.
			string nameProperty = "Customer name";
			newMDCard.Properties.SetPropertyValue( nameProperty, objectName );
			MetadataCardRightPane mdCard = newMDCard.SaveAndDiscardOperations.Save();

			// New object is created. It can now be treated as an existing object.

			// Create a dictionary for expected property values.
			Dictionary<string, string> expectedPropertyValuesByPropertyName = new Dictionary<string, string>();

			// Get the expected property values. We are expecting that these values will not change.
			foreach( string prop in essentialProperties )
			{
				expectedPropertyValuesByPropertyName.Add( prop, mdCard.Properties.GetPropertyValue( prop ) );
			}

			// Change object name.
			mdCard.Properties.SetPropertyValue( nameProperty, "IsForcedTrue renamed" );

			// Create similar dictionary for actual property values.
			Dictionary<string, string> actualPropertyValues = new Dictionary<string, string>();

			// Get the property values after modifying object name.
			foreach( string prop in essentialProperties )
			{
				actualPropertyValues.Add( prop, mdCard.Properties.GetPropertyValue( prop ) );
			}

			// Assert that each property should be unchanged after changing object name.
			foreach( var kvp in expectedPropertyValuesByPropertyName )
			{
				Assert.AreEqual( kvp.Value, mdCard.Properties.GetPropertyValue( kvp.Key ),
					$"Property '{kvp.Key}' value was changed after object name was modified." );
			}

			// Modify object's Telephone number property. It should not affect the Keywords property
			// because the rule has not been triggered again.
			string changedValue = "This should not affect.";
			mdCard.Properties.SetPropertyValue( setValuePropertyPhoneNumber, changedValue );

			// Set the modified value to the expected values. Nothing else should change.
			expectedPropertyValuesByPropertyName[ setValuePropertyPhoneNumber ] = changedValue;

			// Assert that each property should be unchanged after changing Telephone number property.
			foreach( var kvp in expectedPropertyValuesByPropertyName )
			{
				Assert.AreEqual( kvp.Value, mdCard.Properties.GetPropertyValue( kvp.Key ),
					$"Property '{kvp.Key}' value was changed unexpectedly." );
			}

			// Set Department value to trigger configuration rule.
			mdCard.Properties.SetPropertyValue( triggerPropertyDepartment, "Sales" );

			// Assert that both Telephone number and Keywords properties have set values.
			this.AssertSetValueProperty( "Number of Sales is 456", setValuePropertyPhoneNumber, mdCard.Properties );
			this.AssertSetValueProperty( "Number of Sales is 456", setValuePropertyKeywords, mdCard.Properties );

			// Save the object in the end.
			mdCard.SaveAndDiscardOperations.Save();
		}


		[Test]
		public void IsForcedFalsePlaceholderNewObject()
		{
			string configFileName = $"{classID}\\IsForcedFalsePlaceholderNewObject.json";

			EnvironmentSetupHelper.SetupMetadataCardConfiguration( mfContext, configFileName );

			HomePage homePage = this.browserManager.StartTestAtHomePage();

			string objectType = "Customer";

			// Start creating a new object.
			MetadataCardPopout newMDCard = homePage.TopPane.CreateNewObject( objectType );

			// These properties will trigger configuration rules.
			string triggerPropertyCountry = "Country";
			string triggerPropertyDepartment = "Department";

			// These properties will have set values by configuration rules.
			string setValuePropertyCity = "City";
			string setValuePropertyPhoneNumber = "Telephone number";
			string setValuePropertyKeywords = "Keywords";

			// Trigger first rule.
			newMDCard.Properties.SetPropertyValue( triggerPropertyCountry, "USA" );

			// Assert that the City property value is set.
			this.AssertSetValueProperty( "Famous city of USA is Florida", setValuePropertyCity, newMDCard.Properties );

			// Trigger second rule.
			newMDCard.Properties.AddPropertyAndSetValue( triggerPropertyDepartment, "Sales" );

			// Assert that Telephone number value is set by using the Department as placeholder. 
			this.AssertSetValueProperty( "Number of Sales is 456", setValuePropertyPhoneNumber, newMDCard.Properties );

			// Assert that Keywords value is set by using Telephone number as a whole as placeholder.
			this.AssertSetValueProperty( "Number of Sales is 456", setValuePropertyKeywords, newMDCard.Properties );

			// Change values of both trigger properties.
			newMDCard.Properties.SetPropertyValue( triggerPropertyCountry, "Canada" );
			newMDCard.Properties.SetPropertyValue( triggerPropertyDepartment, "Marketing" );

			// The City property still follows the placeholder but doesn't set the Canada SetValue because IsForced=false.
			this.AssertSetValueProperty( "Famous city of Canada is Florida", setValuePropertyCity, newMDCard.Properties );

			// Telephone number and Keywords also follow their placeholders but the Marketing SetValue is not set
			// because IsForced=false.
			this.AssertSetValueProperty( "Number of Marketing is 456", setValuePropertyPhoneNumber, newMDCard.Properties );
			this.AssertSetValueProperty( "Number of Marketing is 456", setValuePropertyKeywords, newMDCard.Properties );


			// Change value of Country trigger property.
			newMDCard.Properties.SetPropertyValue( triggerPropertyCountry, "Germany" );

			// Again, the placeholder is followed.
			this.AssertSetValueProperty( "Famous city of Germany is Florida", setValuePropertyCity, newMDCard.Properties );

			// Now remove values of two set value properties.
			newMDCard.Properties.SetPropertyValue( setValuePropertyCity, "" );
			newMDCard.Properties.SetPropertyValue( setValuePropertyPhoneNumber, "" );

			// Keywords still follows the Telephone number as placeholder so asserting that Keywords 
			// will also be empty.
			this.AssertSetValueProperty( "", setValuePropertyKeywords, newMDCard.Properties );

			// Again set Country value as USA to trigger the rule.
			newMDCard.Properties.SetPropertyValue( triggerPropertyCountry, "USA" );

			// City property was already empty so assert that the rule will set its value.
			this.AssertSetValueProperty( "Famous city of USA is Florida", setValuePropertyCity, newMDCard.Properties );

			// Manually modify Telephone number.
			string manualModification = "Manually modify.";
			newMDCard.Properties.SetPropertyValue( setValuePropertyPhoneNumber, manualModification );

			// Assert that Keywords still uses the placeholder from Telephone number.
			this.AssertSetValueProperty( manualModification, setValuePropertyKeywords, newMDCard.Properties );

			// Again remove values of two set value properties.
			newMDCard.Properties.SetPropertyValue( setValuePropertyCity, "" );
			newMDCard.Properties.SetPropertyValue( setValuePropertyPhoneNumber, "" );

			// Keywords still follows the Telephone number as placeholder so asserting that Keywords 
			// will also be empty.
			this.AssertSetValueProperty( "", setValuePropertyKeywords, newMDCard.Properties );

			newMDCard.Properties.SetPropertyValue( "Customer name", "IsForcedFalse new test" );

			newMDCard.SaveAndDiscardOperations.Save( metadataCardLoadExpected: false );
		}

		[Test]
		public void IsForcedFalsePlaceholderExistingObject()
		{
			string configFileName = $"{classID}\\IsForcedFalsePlaceholderExistingObject.json";

			EnvironmentSetupHelper.SetupMetadataCardConfiguration( mfContext, configFileName );

			HomePage homePage = this.browserManager.StartTestAtHomePage();

			string objectType = "Customer";
			string newObjectName = "IsForcedFalse existing test";

			// Make a search.
			homePage.SearchPane.FilteredQuickSearch( newObjectName, objectType );

			// These properties will trigger configuration rules.
			string triggerPropertyCountry = "Country";
			string triggerPropertyDepartment = "Department";

			// These properties will have set values by configuration rules.
			string setValuePropertyCity = "City";
			string setValuePropertyPhoneNumber = "Telephone number";
			string setValuePropertyKeywords = "Keywords";

			// Start creating a new object. This object creation is basically only a setup step
			// so not much verification is done during object creation.
			MetadataCardPopout newMDCard = homePage.TopPane.CreateNewObject( objectType );

			// Add missing properties.
			newMDCard.Properties.AddProperty( triggerPropertyDepartment );
			newMDCard.Properties.AddProperty( setValuePropertyKeywords );

			// Set values to properties that trigger the configuration rules.
			newMDCard.Properties.SetPropertyValue( triggerPropertyCountry, "USA" );
			newMDCard.Properties.SetPropertyValue( triggerPropertyDepartment, "Marketing" );

			// Remove values from all properties that had their values set by the rules.
			newMDCard.Properties.SetPropertyValue( setValuePropertyCity, "" );
			newMDCard.Properties.SetPropertyValue( setValuePropertyPhoneNumber, "" );
			newMDCard.Properties.SetPropertyValue( setValuePropertyKeywords, "" );

			// Give a name to the object and create it.
			newMDCard.Properties.SetPropertyValue( "Customer name", newObjectName );
			MetadataCardRightPane mdCard = newMDCard.SaveAndDiscardOperations.Save();

			// Set Country to Germany. It shouldn't cause any rule to trigger.
			mdCard.Properties.SetPropertyValue( triggerPropertyCountry, "Germany" );

			// Set country to USA to trigger the rule.
			mdCard.Properties.SetPropertyValue( triggerPropertyCountry, "USA" );

			// Assert that the City property, which was empty, is set value with a placeholder of the Country property.
			this.AssertSetValueProperty( "Famous city of USA is Florida", setValuePropertyCity, mdCard.Properties );

			// Manually set value to Telephone number property.
			string manualValue = "Manually enter value.";
			mdCard.Properties.SetPropertyValue( setValuePropertyPhoneNumber, manualValue );

			// Assert that the Keywords should still stay empty because the second rule has not
			// been triggered and thus the Keywords doesn't follow the telephone number yet as
			// placeholder.
			this.AssertSetValueProperty( "", setValuePropertyKeywords, mdCard.Properties );

			// Set Country as Canada.
			mdCard.Properties.SetPropertyValue( triggerPropertyCountry, "Canada" );

			// Assert that the set value doesn't trigger because the value is not empty but the Country placeholder
			// is still followed.
			this.AssertSetValueProperty( "Famous city of Canada is Florida", setValuePropertyCity, mdCard.Properties );

			// Trigger the second rule.
			mdCard.Properties.SetPropertyValue( triggerPropertyDepartment, "Sales" );

			// Assert that the Telephone number stays as the manually set value because IsForced=false.
			this.AssertSetValueProperty( manualValue, setValuePropertyPhoneNumber, mdCard.Properties );

			// Assert that the Keywords will now be set value from Telephone number because Keywords was empty
			// and the rule was triggered.
			this.AssertSetValueProperty( manualValue, setValuePropertyKeywords, mdCard.Properties );

			mdCard.SaveAndDiscardOperations.Save();
		}

		[Test]
		public void IsForcedTruePlaceholderAndLabel()
		{
			string configFileName = $"{classID}\\IsForcedTruePlaceholderAndLabel.json";

			EnvironmentSetupHelper.SetupMetadataCardConfiguration( mfContext, configFileName );

			HomePage homePage = this.browserManager.StartTestAtHomePage();

			string objectType = "Customer";
			string newObjectName = "IsForcedTrue label";

			MetadataCardPopout newMDCard = homePage.TopPane.CreateNewObject( objectType );

			// This property will trigger configuration rules.
			string triggerPropertyCountry = "Country";

			// This property will have set values by configuration rules.
			string setValuePropertyCity = "City";

			// Trigger the configuration rule by setting Country.
			newMDCard.Properties.SetPropertyValue( triggerPropertyCountry, "USA" );

			// Assert that old label disappears and new label appears. Also assert that the property
			// value is set.
			string cityLabel = "City of USA";
			this.AssertPropertyLabelExistenceInMetadataCard( setValuePropertyCity, false, newMDCard.Properties );
			this.AssertPropertyLabelExistenceInMetadataCard( cityLabel, true, newMDCard.Properties );
			this.AssertSetValueProperty( "Famous city of USA is Florida", cityLabel, newMDCard.Properties );

			// Trigger the other rule by setting another Country value.
			newMDCard.Properties.SetPropertyValue( triggerPropertyCountry, "Canada" );

			// Assert that the label changes again. Also assert that the value is set by using the new rule
			// because IsForced=true.
			cityLabel = "City of Canada";
			this.AssertPropertyLabelExistenceInMetadataCard( setValuePropertyCity, false, newMDCard.Properties );
			this.AssertPropertyLabelExistenceInMetadataCard( cityLabel, true, newMDCard.Properties );
			this.AssertSetValueProperty( "Famous city of Canada is Toronto", cityLabel, newMDCard.Properties );

			// Set Country value which causes the rule's filter not be fulfilled. Still, the set value placeholder 
			// is followed but the label changes back because rule is not active anymore.
			newMDCard.Properties.SetPropertyValue( triggerPropertyCountry, "France" );

			// Assert that the property label changes back to normal. The placeholder causes the City property to
			// change the Country part of the set value.
			this.AssertPropertyLabelExistenceInMetadataCard( setValuePropertyCity, true, newMDCard.Properties );
			this.AssertPropertyLabelExistenceInMetadataCard( cityLabel, false, newMDCard.Properties );
			this.AssertSetValueProperty( "Famous city of France is Toronto", setValuePropertyCity, newMDCard.Properties );

			// Set Country as USA, this will again trigger the rule.
			newMDCard.Properties.SetPropertyValue( triggerPropertyCountry, "USA" );

			// Assert that the label changes and the set value is again triggered.
			cityLabel = "City of USA";
			this.AssertPropertyLabelExistenceInMetadataCard( setValuePropertyCity, false, newMDCard.Properties );
			this.AssertPropertyLabelExistenceInMetadataCard( cityLabel, true, newMDCard.Properties );
			this.AssertSetValueProperty( "Famous city of USA is Florida", cityLabel, newMDCard.Properties );

			// Manually change the value of City.
			string manualChange = "Manually make a change.";
			newMDCard.Properties.SetPropertyValue( cityLabel, manualChange );
			this.AssertSetValueProperty( manualChange, cityLabel, newMDCard.Properties );

			// Set Country to France. It triggers no rule.
			newMDCard.Properties.SetPropertyValue( triggerPropertyCountry, "France" );

			// Assert that now the City property label changes but the placeholder is no longer followed
			// and thus the manual change remains.
			this.AssertPropertyLabelExistenceInMetadataCard( setValuePropertyCity, true, newMDCard.Properties );
			this.AssertPropertyLabelExistenceInMetadataCard( cityLabel, false, newMDCard.Properties );
			this.AssertSetValueProperty( manualChange, setValuePropertyCity, newMDCard.Properties );

			// Set name to the object and create it.
			newMDCard.Properties.SetPropertyValue( "Customer name", newObjectName );
			newMDCard.SaveAndDiscardOperations.Save( metadataCardLoadExpected: false );
		}

		[Test]
		[TestCase(
			"Dynamic MLtext test",
			"MultilineTextDynamicPlaceholders.json",
			"MDC_Text_ML;MDC_Text_ML2;MDC_Text_ML3;MDC_Text_ML4;MDC_Text_ML5;MDC_Text_ML6;MDC_Text_ML7;MDC_Text_ML8;MDC_Text_ML9",
			"USA;Sales;89;98.00;Yes;4/19/2018;09:00:00 AM;Static text;Static multiline\r\ntext" )]
		[TestCase(
			"Dynamic text test",
			"TextDynamicPlaceholders.json",
			"MDC_Text;MDC_Text2;MDC_Text3;MDC_Text4;MDC_Text5;MDC_Text6;MDC_Text7;MDC_Text8;MDC_Text9",
			"USA;Sales;89;98.00;Yes;4/19/2018;09:00:00 AM;Static text;Static multiline\r\ntext" )]
		[TestCase(
			"Dynamic datatypes test",
			"SameDataTypesDynamicPlaceholders.json",
			"MDC_ChooseFromListMSCountries;MDC_ChooseFromListDepartments;MDC_Integer;MDC_Real;MDC_Boolean;MDC_Date;MDC_Time;MDC_Text;MDC_Text_ML",
			"USA;Sales;89;98.00;Yes;4/19/2018;09:00:00 AM;Static text;Static multiline\r\ntext" )]
		public void DynamicPlaceholdersInPropertyTypes(
			string objectName,
			string configFile,
			string propertiesString,
			string expectedPropertyValuesString )
		{
			string configFileName = $"{classID}\\{configFile}";

			EnvironmentSetupHelper.SetupMetadataCardConfiguration( mfContext, configFileName );

			HomePage homePage = this.browserManager.StartTestAtHomePage();

			// Start creating a document.
			string objectType = "Document";
			string template = "Microsoft Word Document (.docx)";
			MetadataCardPopout newMDCard = homePage.TopPane.CreateNewObjectFromTemplate( objectType, template );

			// Set class "Other Document". This will trigger a rule that adds multiple properties to the metadata card
			// and sets their values. The property count should be in total 22, after setting the class.
			newMDCard.Properties.SetPropertyValue( "Class", "Other Document", expectedPropertyCountAfterSettingValue: 22 );

			// Get expected properties and their values from the test data strings.
			List<string> properties = propertiesString.Split( ';' ).ToList();
			List<string> expectedPropertyValues = expectedPropertyValuesString.Split( ';' ).ToList();

			// Assert that all properties get their set values. They are using placeholders from
			// various property data types.
			for( int i = 0; i < properties.Count; ++i )
			{
				this.AssertSetValueProperty( expectedPropertyValues[ i ], properties[ i ], newMDCard.Properties );
			}

			// Set property name, check in immediately, and save
			newMDCard.Properties.SetPropertyValue( "Name or title", objectName );
			newMDCard.CheckInImmediatelyClick();
			newMDCard.SaveAndDiscardOperations.Save( metadataCardLoadExpected: false );

		}

		[Test]
		public void NameOrTitleAsPlaceholder()
		{
			string configFileName = $"{classID}\\NameOrTitleAsPlaceholder.json";

			EnvironmentSetupHelper.SetupMetadataCardConfiguration( mfContext, configFileName );

			HomePage homePage = this.browserManager.StartTestAtHomePage();

			// Start creating a document.
			string objectType = "Document";
			string template = "Text Document (.txt)";
			MetadataCardPopout newMDCard = homePage.TopPane.CreateNewObjectFromTemplate( objectType, template );

			// Set class "Other Document" which will trigger the rule. Setting class, so new properties will appear.
			// Thus calling the overloaded method which will wait until properties appear to the metadata card.
			newMDCard.Properties.SetPropertyValue( "Class", "Other Document", expectedPropertyCountAfterSettingValue: 4 );

			// Name or title property is the placeholder.
			string placeholderNameProperty = "Name or title";

			// Description property has "Name or title" as placeholder in the set value configuration.
			string setValuePropertyDescription = "Description";

			// Assert that the set value is used but name or title is still empty. Thus the placeholder part is empty.
			this.AssertSetValueProperty( "Name or title is: ", setValuePropertyDescription, newMDCard.Properties );

			// Set name.
			newMDCard.Properties.SetPropertyValue( placeholderNameProperty, "TITLE" );

			// Assert that Description property should follow the placeholder.
			this.AssertSetValueProperty( "Name or title is: TITLE", setValuePropertyDescription, newMDCard.Properties );

			// Change the Description manually which causes the placeholder no longer be followed.
			string manualChange = "Manually change value";
			newMDCard.Properties.SetPropertyValue( setValuePropertyDescription, manualChange );

			// Change the name.
			newMDCard.Properties.SetPropertyValue( placeholderNameProperty, "New title" );

			// Assert that Description doesn't change because the placeholder is no longer followed.
			this.AssertSetValueProperty( manualChange, setValuePropertyDescription, newMDCard.Properties );

			// Trigger the rule again by changing class and then back to Other Document.
			newMDCard.Properties.SetPropertyValue( "Class", "Unclassified Document" );
			newMDCard.Properties.SetPropertyValue( "Class", "Other Document", expectedPropertyCountAfterSettingValue: 4 );

			// Assert that Description doesn't change because the Description already has a value.
			this.AssertSetValueProperty( manualChange, setValuePropertyDescription, newMDCard.Properties );

			// Remove value from Description.
			newMDCard.Properties.SetPropertyValue( setValuePropertyDescription, "" );

			// Assert that the Description is empty.
			this.AssertSetValueProperty( "", setValuePropertyDescription, newMDCard.Properties );

			// Change the name.
			newMDCard.Properties.SetPropertyValue( placeholderNameProperty, "Change the title" );

			// Assert that the Description is still empty because it doesn't currently have the
			// placeholder. It doesn't have the placeholder because its previous value was not overwritten
			// by the set value.
			this.AssertSetValueProperty( "", setValuePropertyDescription, newMDCard.Properties );

			// Trigger the rule again by changing class and setting it back to Other Document.
			newMDCard.Properties.SetPropertyValue( "Class", "Unclassified Document" );
			newMDCard.Properties.SetPropertyValue( "Class", "Other Document", expectedPropertyCountAfterSettingValue: 4 );

			// Assert that now the Description sets value because it was empty and the rule was triggered.
			this.AssertSetValueProperty( "Name or title is: Change the title", setValuePropertyDescription, newMDCard.Properties );

			// Change the name.
			newMDCard.Properties.SetPropertyValue( placeholderNameProperty, "Final title" );

			// Assert that the Description value follows the placeholder.
			this.AssertSetValueProperty( "Name or title is: Final title", setValuePropertyDescription, newMDCard.Properties );

			// Check in immediately and save.
			newMDCard.CheckInImmediatelyClick();
			newMDCard.SaveAndDiscardOperations.Save( metadataCardLoadExpected: false );
		}

		[Test]
		public void NestedPlaceholderRuleIsForcedFalse()
		{
			string configFileName = $"{classID}\\NestedPlaceholderRuleIsForcedFalse.json";

			EnvironmentSetupHelper.SetupMetadataCardConfiguration( mfContext, configFileName );

			HomePage homePage = this.browserManager.StartTestAtHomePage();

			string objectType = "Document";
			string template = "Microsoft Excel Worksheet (.xlsx)";

			// This property will trigger the nested set value child rule.
			string nestedTriggerPropertyDepartment = "Department";

			// The value of this property will be set by both rules (parent and child).
			string setValuePropertyKeywords = "Keywords";

			// Start creating new document.
			MetadataCardPopout newMDCard = homePage.TopPane.CreateNewObjectFromTemplate( objectType, template );

			// Set class as Other Document to trigger the parent rule. Keywords has placeholder of Name or title property.
			newMDCard.Properties.SetPropertyValue( "Class", "Other Document", expectedPropertyCountAfterSettingValue: 4 );

			// Set object name.
			string objectName = "Nested rule IsForcedFalse";
			newMDCard.Properties.SetPropertyValue( "Name or title", objectName );

			// Assert that the placeholder follows Name or title property.
			this.AssertSetValueProperty( objectName, setValuePropertyKeywords, newMDCard.Properties );

			// Trigger the child rule.
			newMDCard.Properties.AddPropertyAndSetValue( nestedTriggerPropertyDepartment, "Sales" );

			// Assert that the Keywords doesn't change because child rule has Isforced=false.
			this.AssertSetValueProperty( objectName, setValuePropertyKeywords, newMDCard.Properties );

			// Change the placeholder property value (which happens to also be the trigger property).
			newMDCard.Properties.SetPropertyValue( nestedTriggerPropertyDepartment, "Marketing" );

			// Assert that again the Keywords doesn't change because Department is not used as
			// placeholder.
			this.AssertSetValueProperty( objectName, setValuePropertyKeywords, newMDCard.Properties );

			// Create the object.
			newMDCard.CheckInImmediatelyClick();
			newMDCard.SaveAndDiscardOperations.Save( metadataCardLoadExpected: false );
		}

		[Test]
		public void NestedPlaceholderRuleIsForcedTrue()
		{
			string configFileName = $"{classID}\\NestedPlaceholderRuleIsForcedTrue.json";

			EnvironmentSetupHelper.SetupMetadataCardConfiguration( mfContext, configFileName );

			HomePage homePage = this.browserManager.StartTestAtHomePage();

			string objectType = "Document";
			string template = "Multi-File Document";

			// This property will trigger the nested set value child rule.
			string nestedTriggerPropertyDepartment = "Department";

			// The value of this property will be set by both rules (parent and child).
			string setValuePropertyKeywords = "Keywords";

			// Start creating new document.
			MetadataCardPopout newMDCard = homePage.TopPane.CreateNewObjectFromTemplate( objectType, template );

			// Set class as Other Document to trigger the parent rule. Keywords has placeholder of Name or title property.
			newMDCard.Properties.SetPropertyValue( "Class", "Other Document", expectedPropertyCountAfterSettingValue: 4 );

			// Set object name.
			string objectName = "Nested rule test IsforcedTrue";
			newMDCard.Properties.SetPropertyValue( "Name or title", objectName );

			// Assert that the placeholder follows Name or title property.
			this.AssertSetValueProperty( objectName, setValuePropertyKeywords, newMDCard.Properties );

			// Trigger the child rule.
			string triggerValue = "Sales";
			newMDCard.Properties.AddPropertyAndSetValue( nestedTriggerPropertyDepartment, triggerValue );

			// Assert that the Keywords value will set as the Department value because the child rule
			// has IsForced=true.
			this.AssertSetValueProperty( triggerValue, setValuePropertyKeywords, newMDCard.Properties );

			// Change the Department value.
			string placeholderValue = "Marketing";
			newMDCard.Properties.SetPropertyValue( nestedTriggerPropertyDepartment, placeholderValue );

			// Assert that the Keywords follows the Department placeholder.
			this.AssertSetValueProperty( placeholderValue, setValuePropertyKeywords, newMDCard.Properties );

			// Create the object.
			newMDCard.CheckInImmediatelyClick();
			newMDCard.SaveAndDiscardOperations.Save( metadataCardLoadExpected: false );
		}

		// TODO: This test should probably be ignored with Firefox. There seems to be some issue with 
		// SetMultiSelectLookupPropertyValue method and the specific metadata card configuration
		// in this test case. The Javascript click seems to remove values from the MSLU when it
		// should add the values. Also, the set value property "MDC_ChooseFromListMSCountries"
		// gets empty values inserted to it in the process. No issues when running test manually
		// with Firefox and no issues with SetMultiSelectLookupPropertyValue when this particular
		// metadata card configuration is not active.
		[Test]
		public void MultiSelectLookupPlaceholderIsForcedTrue()
		{
			string configFileName = $"{classID}\\MultiSelectLookupPlaceholderIsForcedTrue.json";

			EnvironmentSetupHelper.SetupMetadataCardConfiguration( mfContext, configFileName );

			HomePage homePage = this.browserManager.StartTestAtHomePage();

			string objectType = "Customer";

			// This property will be used as placeholder.
			string placeholderPropertyCountry = "Country";

			// This property will have its value set by the placeholder.
			string setValuePropertyCustomCountryMslu = "MDC_ChooseFromListMSCountries";

			// Start creating new object.
			MetadataCardPopout newMDCard = homePage.TopPane.CreateNewObject( objectType );

			// Change some values in the Country property.
			newMDCard.Properties.SetMultiSelectLookupPropertyValue( placeholderPropertyCountry, "China", replaceThisValue: "Canada" );
			newMDCard.Properties.SetMultiSelectLookupPropertyValue( placeholderPropertyCountry, "France", replaceThisValue: "USA" );

			// Assert that the set value property should follow the placeholder.
			List<string> expectedMSLUValues = new List<string> { "France", "China", "United Kingdom" };
			this.AssertSetValueMsluProperty( expectedMSLUValues, setValuePropertyCustomCountryMslu, newMDCard.Properties );

			// Activate edit mode of the set value property but don't change anything.
			// This should not count as modification and thus the placeholder should still be active.
			newMDCard.Properties.CanPropertyEditModeBeActivated( setValuePropertyCustomCountryMslu );

			// Change again some values in Country property.
			newMDCard.Properties.SetMultiSelectLookupPropertyValue( placeholderPropertyCountry, "Germany", replaceThisValue: "United Kingdom" );
			newMDCard.Properties.SetMultiSelectLookupPropertyValue( placeholderPropertyCountry, "USA", replaceThisValue: "China" );

			// Assert that the changes are followed by the placeholder in the set value property.
			List<string> expectedMSLUValues2 = new List<string> { "France", "USA", "Germany" };
			this.AssertSetValueMsluProperty( expectedMSLUValues2, setValuePropertyCustomCountryMslu, newMDCard.Properties );

			// Now manually set values to the set value property. This should stop the placeholder to affect,
			newMDCard.Properties.SetMultiSelectLookupPropertyValue( setValuePropertyCustomCountryMslu, "China", replaceThisValue: "Germany" );
			newMDCard.Properties.SetMultiSelectLookupPropertyValue( setValuePropertyCustomCountryMslu, "United Kingdom", replaceThisValue: "France" );

			// These are the new expected values in the set value property after the changes.
			List<string> expectedMSLUValues3 = new List<string> { "United Kingdom", "USA", "China" };

			// Now setting the placeholder values which should not affect the set value property anymore.
			newMDCard.Properties.SetMultiSelectLookupPropertyValue( placeholderPropertyCountry, "Canada", replaceThisValue: "USA" );
			newMDCard.Properties.SetMultiSelectLookupPropertyValue( placeholderPropertyCountry, "United Kingdom", replaceThisValue: "Germany" );

			// Assert that the manually set values remain.
			this.AssertSetValueMsluProperty( expectedMSLUValues3, setValuePropertyCustomCountryMslu, newMDCard.Properties );

			// Set object name property. This should not affect the placeholders either.
			newMDCard.Properties.SetPropertyValue( "Customer name", "MSLU placeholder" );

			// Assert that still the manually set values remain.
			this.AssertSetValueMsluProperty( expectedMSLUValues3, setValuePropertyCustomCountryMslu, newMDCard.Properties );

			// Create the object.
			newMDCard.SaveAndDiscardOperations.Save( metadataCardLoadExpected: false );
		}

		[Test]
		public void BooleanPlaceholderIsForcedTrue()
		{
			string configFileName = $"{classID}\\BooleanPlaceholderIsForcedTrue.json";

			EnvironmentSetupHelper.SetupMetadataCardConfiguration( mfContext, configFileName );

			HomePage homePage = this.browserManager.StartTestAtHomePage();

			string objectType = "Project";
			string objectName = "Boolean placeholder test";

			// Go to search view.
			homePage.SearchPane.FilteredQuickSearch( objectName, objectType );

			// This property will be used as placeholder.
			string placeholderPropertyInProgress = "In progress";

			// This property will have its value set by the placeholder.
			string setValuePropertyCustomBoolean = "MDC_Boolean";

			// Start creating new object.
			MetadataCardPopout newMDCard = homePage.TopPane.CreateNewObject( objectType );

			// Set class to trigger the rule.
			newMDCard.Properties.SetPropertyValue( "Class", "Internal Project", 5 );

			// Set value to In progress property.
			newMDCard.Properties.SetPropertyValue( placeholderPropertyInProgress, "Yes" );

			// Assert that the value was set to the set value property.
			this.AssertSetValueProperty( "Yes", setValuePropertyCustomBoolean, newMDCard.Properties );

			// Change the value of In progress.
			newMDCard.Properties.SetPropertyValue( placeholderPropertyInProgress, "No" );

			// Assert that again the set value property value changes to match the placeholder.
			this.AssertSetValueProperty( "No", setValuePropertyCustomBoolean, newMDCard.Properties );

			// Manually modify the value of the set value property.
			newMDCard.Properties.SetPropertyValue( setValuePropertyCustomBoolean, "Yes" );

			// Toggle the value of In progress to "Yes" and then "No".
			newMDCard.Properties.SetPropertyValue( placeholderPropertyInProgress, "Yes" );
			newMDCard.Properties.SetPropertyValue( placeholderPropertyInProgress, "No" );

			// Assert that the set value property doesn't follow the In progress placeholder anymore
			// because the set value property was manually modified. Thus, the value is still "Yes".
			this.AssertSetValueProperty( "Yes", setValuePropertyCustomBoolean, newMDCard.Properties );

			// Set value to some other property.
			newMDCard.Properties.SetPropertyValue( "Project manager", "Bill Richards" );

			// Assert that the set value property was not affected.
			this.AssertSetValueProperty( "Yes", setValuePropertyCustomBoolean, newMDCard.Properties );

			// Set object name.
			newMDCard.Properties.SetPropertyValue( "Name or title", objectName );

			// Assert that the set value property was not affected.
			this.AssertSetValueProperty( "Yes", setValuePropertyCustomBoolean, newMDCard.Properties );

			// Create the object.
			MetadataCardRightPane mdCard = newMDCard.SaveAndDiscardOperations.Save();

			// Assert that the set value property cannot be removed because it was added
			// as an additional property.
			Assert.False( mdCard.Properties.IsPropertyRemovable( setValuePropertyCustomBoolean ),
				$"Additional property '{setValuePropertyCustomBoolean}' was removable when user is not supposed to be able to remove it." );
		}

		[Test]
		public void ClassChangeRemovesEmptyProperty()
		{
			string configFileName = $"{classID}\\ClassChangeRemovesEmptyProperty.json";

			EnvironmentSetupHelper.SetupMetadataCardConfiguration( mfContext, configFileName );

			HomePage homePage = this.browserManager.StartTestAtHomePage();

			// Start creating new document from template.
			string objectType = "Document";
			string template = "Microsoft PowerPoint Presentation (.pptx)";
			MetadataCardPopout newMDCard = homePage.TopPane.CreateNewObjectFromTemplate( objectType, template );

			// Set class.
			newMDCard.Properties.SetPropertyValue( "Class", "Unclassified Document" );

			// Add two properties, which have set value rules.
			string setValuePropertyMyText = "myText";
			string setValuePropertyMDCText = "MDC_Text";
			newMDCard.Properties.AddProperty( setValuePropertyMyText );
			newMDCard.Properties.AddProperty( setValuePropertyMDCText );

			// Remove the value from first set value property.
			newMDCard.Properties.SetPropertyValue( setValuePropertyMyText, "" );

			// Change class.
			newMDCard.Properties.SetPropertyValue( "Class", "Other Document", expectedPropertyCountAfterSettingValue: 5 );

			// Assert that the first set value property was removed because it had manually set empty value.
			Assert.False( newMDCard.Properties.IsPropertyInMetadataCard( setValuePropertyMyText ),
				$"Property '{setValuePropertyMyText}' was in metadata card when it was not supposed to be." );

			// Assert that the second set value property remains because it had empty value which was a placeholder.
			Assert.True( newMDCard.Properties.IsPropertyInMetadataCard( setValuePropertyMDCText ),
				$"Property '{setValuePropertyMDCText}' was not in metadata card when it was supposed to be." );

			// Add property which is the placeholder of the MDC_Text property.
			string placeholderPropertyProductOrService = "Product or service";
			string placeholderTextValue = "Copy this value";
			newMDCard.Properties.AddPropertyAndSetValue( placeholderPropertyProductOrService, placeholderTextValue );

			// Assert that the set value property follows the placeholder.
			Assert.AreEqual( placeholderTextValue, newMDCard.Properties.GetPropertyValue( setValuePropertyMDCText ) );
			this.AssertSetValueProperty( placeholderTextValue, setValuePropertyMDCText, newMDCard.Properties );

			// Manually empty the set value property.
			newMDCard.Properties.SetPropertyValue( setValuePropertyMDCText, "" );

			// Change class.
			newMDCard.Properties.SetPropertyValue( "Class", "Unclassified Document", expectedPropertyCountAfterSettingValue: 3 );

			// Assert that the manually emptied set value property is removed.
			Assert.False( newMDCard.Properties.IsPropertyInMetadataCard( setValuePropertyMDCText ),
				$"Property '{setValuePropertyMDCText}' was in metadata card when it was not supposed to be." );

			// Create the object.
			string objectName = "Empty properties test";
			newMDCard.Properties.SetPropertyValue( "Name or title", objectName );
			newMDCard.CheckInImmediatelyClick();
			newMDCard.SaveAndDiscardOperations.Save( metadataCardLoadExpected: false );
		}

		[Test]
		public void ChainOfPlaceholders()
		{
			string configFileName = $"{classID}\\ChainOfPlaceholders.json";

			EnvironmentSetupHelper.SetupMetadataCardConfiguration( mfContext, configFileName );

			HomePage homePage = this.browserManager.StartTestAtHomePage();

			string objectType = "Customer";

			// Start creating new object.
			MetadataCardPopout newMDCard = homePage.TopPane.CreateNewObject( objectType );

			// This property's value will be set to other properties.
			string placeholderPropertyCountry = "Country";

			// This property has placeholder from Country property.
			string setValuPropertyCustomCountryMslu = "MDC_ChooseFromListMSCountries";

			// This property has placeholder from the previous property "MDC_ChooseFromListMSCountries".
			string setValueMLTextPropertyDescription = "Description";

			// Assert that properties have set values because rule is immediately triggered.
			string expectedMLTextSetValue = $"Current user: {this.username}, Country placeholder USA; Canada; United Kingdom";
			List<string> expectedMSLUSetValue = new List<string> { "USA", "Canada", "United Kingdom" };
			this.AssertSetValueProperty( expectedMLTextSetValue, setValueMLTextPropertyDescription, newMDCard.Properties );
			this.AssertSetValueMsluProperty( expectedMSLUSetValue, setValuPropertyCustomCountryMslu, newMDCard.Properties );

			// Change MSLU value in placeholder property Country.
			newMDCard.Properties.SetMultiSelectLookupPropertyValue( placeholderPropertyCountry, "China", replaceThisValue: "Canada" );

			// Assert that both the MSLU- and Multiline text set value properties follow their placeholders.
			string expectedMLTextSetValue2 = $"Current user: {this.username}, Country placeholder USA; China; United Kingdom";
			List<string> expectedMSLUSetValue2 = new List<string> { "USA", "China", "United Kingdom" };
			this.AssertSetValueProperty( expectedMLTextSetValue2, setValueMLTextPropertyDescription, newMDCard.Properties );
			this.AssertSetValueMsluProperty( expectedMSLUSetValue2, setValuPropertyCustomCountryMslu, newMDCard.Properties );

			// Activate edit mode in both set value properties but don't change values.
			newMDCard.Properties.CanPropertyEditModeBeActivated( setValueMLTextPropertyDescription );
			newMDCard.Properties.CanPropertyEditModeBeActivated( setValuPropertyCustomCountryMslu );

			// Again change a MSLU value in the placeholder property Country.
			newMDCard.Properties.SetMultiSelectLookupPropertyValue( placeholderPropertyCountry, "France", replaceThisValue: "United Kingdom" );

			// Assert that both set value properties still follow their placeholders.
			string expectedMLTextSetValue3 = $"Current user: {this.username}, Country placeholder USA; China; France";
			List<string> expectedMSLUSetValue3 = new List<string> { "USA", "China", "France" };
			this.AssertSetValueProperty( expectedMLTextSetValue3, setValueMLTextPropertyDescription, newMDCard.Properties );
			this.AssertSetValueMsluProperty( expectedMSLUSetValue3, setValuPropertyCustomCountryMslu, newMDCard.Properties );

			// Manually change a value in the MSLU set value property.
			newMDCard.Properties.SetMultiSelectLookupPropertyValue( setValuPropertyCustomCountryMslu, "Germany", replaceThisValue: "USA" );

			// Assert that the multiline text set value property follows the value of "MDC_ChooseFromListMSCountries".
			string expectedMLTextSetValue4 = $"Current user: {this.username}, Country placeholder Germany; China; France";
			this.AssertSetValueProperty( expectedMLTextSetValue4, setValueMLTextPropertyDescription, newMDCard.Properties );

			// Change value of the original placeholder property Country.
			newMDCard.Properties.SetMultiSelectLookupPropertyValue( placeholderPropertyCountry, "Canada", replaceThisValue: "France" );

			// Now assert that the MSLU value doesn't follow placeholder because its value was manually modified. Also, the Description
			// doesn't change because it follows the "MDC_ChooseFromListMSCountries" property and not the Country.
			string expectedMLTextSetValue5 = expectedMLTextSetValue4;
			List<string> expectedMSLUSetValue5 = new List<string> { "Germany", "China", "France" };
			this.AssertSetValueProperty( expectedMLTextSetValue5, setValueMLTextPropertyDescription, newMDCard.Properties );
			this.AssertSetValueMsluProperty( expectedMSLUSetValue5, setValuPropertyCustomCountryMslu, newMDCard.Properties );

			// Manually modify the "MDC_ChooseFromListMSCountries" property again.
			newMDCard.Properties.SetMultiSelectLookupPropertyValue( setValuPropertyCustomCountryMslu, "United Kingdom", replaceThisValue: "China" );

			// Assert that the Description changes because it still follows the placeholder.
			string expectedMLTextSetValue6 = $"Current user: {this.username}, Country placeholder Germany; United Kingdom; France";
			this.AssertSetValueProperty( expectedMLTextSetValue6, setValueMLTextPropertyDescription, newMDCard.Properties );

			// Manually change the Description property.
			string manualTextChange = "Manual change";
			newMDCard.Properties.SetPropertyValue( setValueMLTextPropertyDescription, manualTextChange );

			// Activate edit mode of some property.
			newMDCard.Properties.CanPropertyEditModeBeActivated( "Customer name" );

			// Manually change the Description's value back to what it was before the modification.
			newMDCard.Properties.SetPropertyValue( setValueMLTextPropertyDescription, expectedMLTextSetValue6 );

			// Manually change the value of "MDC_ChooseFromListMSCountries" property.
			newMDCard.Properties.SetMultiSelectLookupPropertyValue( setValuPropertyCustomCountryMslu, "USA", replaceThisValue: "France" );

			// Assert that the Description doesn't follow the value of "MDC_ChooseFromListMSCountries" because of the manual modification.
			string expectedMLTextSetValue7 = expectedMLTextSetValue6;
			this.AssertSetValueProperty( expectedMLTextSetValue7, setValueMLTextPropertyDescription, newMDCard.Properties );

			// Set value to some property.
			newMDCard.Properties.SetPropertyValue( "Customer name", "Chain of placeholders" );

			// Assert that the set value properties were not changed.
			string expectedMLTextSetValue8 = expectedMLTextSetValue7;
			List<string> expectedMSLUSetValue8 = new List<string> { "Germany", "United Kingdom", "USA" };
			this.AssertSetValueProperty( expectedMLTextSetValue8, setValueMLTextPropertyDescription, newMDCard.Properties );
			this.AssertSetValueMsluProperty( expectedMSLUSetValue8, setValuPropertyCustomCountryMslu, newMDCard.Properties );

			// Create the object.
			newMDCard.SaveAndDiscardOperations.Save( metadataCardLoadExpected: false );
		}

		[Test]
		public void SetValueClearsFilledProperties()
		{
			string configFileName = $"{classID}\\SetValueClearsFilledProperties.json";

			EnvironmentSetupHelper.SetupMetadataCardConfiguration( mfContext, configFileName );

			HomePage homePage = this.browserManager.StartTestAtHomePage();

			string objectType = "Document";
			string template = "Bitmap Image (.bmp)";

			// Start creating a new object.
			MetadataCardPopout newMDCard = homePage.TopPane.CreateNewObjectFromTemplate( objectType, template );

			// Set class.
			newMDCard.Properties.SetPropertyValue( "Class", "Other Document", expectedPropertyCountAfterSettingValue: 4 );

			// Add properties and set values to these specific properties.
			newMDCard.Properties.AddPropertyAndSetValue( "Department", "Sales" );
			newMDCard.Properties.AddPropertyAndSetValue( "Employee", "Bill Richards" );
			newMDCard.Properties.SetPropertyValue( "Keywords", "Text value" );
			newMDCard.Properties.SetPropertyValue( "Description", "MLText value" );

			// Change class to Unclassified property which will trigger a rule. It will clear
			// the values of the Department, Employee, Keywords, and Description.
			newMDCard.Properties.SetPropertyValue( "Class", "Unclassified Document" );

			// Properties whose values are emptied.
			List<string> properties = new List<string>
			{
				"Keywords", "Description", "Department", "Employee"
			};

			// Go through the properties.
			foreach( string property in properties )
			{
				// Assert that the rule emptied the property value.
				this.AssertSetValueProperty( "", property, newMDCard.Properties );
			}

			// Assert that metadata card description is visible.
			string expectedDescription = "Clear Description, Keywords, Emplyee and Department";
			Assert.AreEqual( expectedDescription, newMDCard.ConfigurationOperations.MetadataCardDescription,
				$"Mismatch between expected and actual metadata card description." );

			// Create the object.
			newMDCard.Properties.SetPropertyValue( "Name or title", "Clear filled properties test" );
			newMDCard.CheckInImmediatelyClick();
			newMDCard.SaveAndDiscardOperations.Save( metadataCardLoadExpected: false );
		}
	}
}
