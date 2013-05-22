using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Reflection;
using CsvHelper;
using Metanga.SoftwareDevelopmentKit.Proxy;
using Metanga.SoftwareDevelopmentKit.Rest;

namespace Metanga.Metadata
{
    /// <summary>
    /// a public class for metadata.
    /// </summary>
    public class MetadataClass
    {

        private static List<object> dynamicEntity = new List<object> ( );
        public MetangaClient Client { get; set; }

        public Type SelectedEntityName { get; set; }

        public MetangaClient Metadata ( )
        {
            Client = OpenMetangaClient ( );
            if ( Client == null )
            {
                EndExample ( );
            }
            return Client;
        }

        /// <summary>
        /// Reads the available entities.
        /// </summary>
        /// <returns></returns>
        public IEnumerable<Type> ReadAvailableEntities ( )
        {
            var entityType = typeof ( Entity );
            if ( Client == null ) return null;

            return Assembly.GetAssembly ( entityType ).GetTypes ( ).Where ( entityType.IsAssignableFrom ).ToList ( );
        }

        /// <summary>
        /// Reads the selected entity's methods.
        /// </summary>
        /// <param name="selectedEntity">The selected entity.</param>
        /// <returns></returns>
        public IEnumerable<PropertyInfo> ReadSelectedEntitysMethods ( string selectedEntity )
        {
            var entityType = typeof ( Entity );
            if ( Client == null ) return null;

            var nextTypes = Assembly.GetAssembly ( entityType ).GetTypes ( ).Where ( entityType.IsAssignableFrom ).ToList ( );
            var propertyArray = new List<PropertyInfo> ( );
            foreach ( PropertyInfo field in nextTypes.Where ( nextType => nextType.Name.Equals ( selectedEntity ) ).SelectMany ( nextType => nextType.GetProperties ( ) ) )
            {
                propertyArray.Add ( field );
                //Console.WriteLine ( string.Format ( "{0}", field.Name ) );
            }


            //IEnumerable<PropertyInfo> properties =   nextTypes.Where ( nextType => nextType.Name.Equals ( selectedEntity ) ).SelectMany ( nextType => nextType.GetProperties ( ).ToList ( ) );
            return propertyArray;
        }

        /// <summary>
        /// Opens a connection to the Metanga service. If there is a MetangaException, it
        /// displays an appropriate message in the console.
        /// </summary>
        /// <returns></returns>
        private static MetangaClient OpenMetangaClient ( )
        {
            var metangaAddress = ConfigurationManager.AppSettings [ "MetangaAddress" ];
            var username = ConfigurationManager.AppSettings [ "MetangaUsername" ];
            var password = ConfigurationManager.AppSettings [ "MetangaPassword" ];
            var address = new Uri ( metangaAddress, UriKind.Absolute );
            MetangaClient client;
            try
            {
                client = MetangaClient.Initialize ( address, username, password, MetangaContentType.Json );
            }
            catch ( MetangaException e )
            {
                throw new Exception ( String.Format ( "An error has occurred while connecting to Metanga: Id={0}, Message={1}", e.ErrorId, e.Message ) );
            }
            return client;
        }

        /// <summary>
        /// Before exiting, ask a user to press a key to finish and wait for the key.
        /// Very useful when running from Visual Studio
        /// </summary>
        private static string EndExample ( )
        {
            return ( "Press any key to finish" );
        }

        public List<object> CreateObject ( string language, IEnumerable<Entity> selectedEntityType )
        {
            foreach ( Entity product in selectedEntityType )
            {
                var rto = new RunTimeObject ( );
                object newDynamicObject = rto.CreateNewObject ( product, language, SelectedEntityName );
                dynamicEntity.Add ( newDynamicObject );
            }
            return dynamicEntity;
        }

        public object ReturnSampleData ( PropertyInfo propertType )
        {
            switch ( ( propertType.GetGetMethod ( ).ReturnParameter.ParameterType ).FullName )
            {
                case "System.Decimal":
                    return ( Decimal.One/3*3 );
                    break;
                case "System.Boolean":
                    return true;
                    break;
                case "System.String":
                    return "This is sample data for testing purposes only.";
                    break;
                case "System.DateTime":
                    return DateTime.UtcNow;
                    break;
                case "System.Int32":
                    return new Random ( ).Next ( 99 );
                    break;
                case "System.Nullable'1":
                    return "This sample value is not null.";
                    break;
                //case "EventType":
                //    break;
                //case "ExtensionDataObject":
                //    break;
                //case "Dictionary'2":
                //    break;
                //case "NotificationEndpointConfiguration":
                //    break;
                //case "AuthenticationConfiguration":
                //    break;
                //case "PaymentInstrumentMasked":
                //    break;
                //case "PriceSchedule":
                //    break;
                //case "ProductModel":
                //    break;

                //case "PackageProduct":
                //    break;
                //case "PaymentInfoStatus":
                //    break;
                //case "Package":
                //    break;
                //case "Product":
                //    break;
                //case "SubscriptionPackageProduct":
                //    break;
                //case "ManualPaymentOperation":
                //    break;
                //case "CreditCardType":
                //    break;
                //case "ElectronicPaymentOperation":
                //    break;
                //case "InvoiceState":
                //    break;
                //case "EntityTypeColumn":
                //    break;
                default:
                    return null;
                    break;
            }
        }

        /// <summary>
        /// Creates the sample data.
        /// </summary>
        /// <param name="entity">The entity.</param>
        public Type CreateSampleData ( object entity )
        {

            var aProps = entity.GetType ( ).GetProperties ( );
            var bProps = entity.GetType ( );

            foreach ( var bProp in bProps.GetProperties ( ) )
            {
                var aProp = aProps.SingleOrDefault ( x => x.Name == bProp.Name );
                if ( aProp != null )
                {
                    //var dtoVal = bProp.GetValue ( entity, null );

                    var dtoVal = ReturnSampleData ( aProp );
                    aProp.SetValue ( entity, dtoVal ); // ERROR HERE
                }
            }
            return bProps;
        }

        public void WriteFile ( string fileName, string entityName, Type properties )
        {
            using ( var textWriter = File.CreateText ( string.Format ( "{0}_{1}.csv", fileName, entityName ) ) )
            using ( var writer = new CsvWriter ( textWriter ) )
            {
                //List<PropertyInfo> records =  properties.ToList ( );
                //foreach ( var record in records )
                //{
                //    writer.WriteHeader<PropertyInfo> ( );

                //}
                writer.WriteRecords ( properties.GetProperties ( ) );
            }
        }

        public IEnumerable<Entity> ClientCallGetEntity ( )
        {
            IEnumerable<Entity> bulkEntity = null;
            switch ( SelectedEntityName.Name )
            {
                case "Entity":
                    bulkEntity= Client.RetrieveEntitiesBulk<Entity> ( );
                    break;
                case "Payment":
                    bulkEntity= Client.RetrieveEntitiesBulk<Payment> ( );
                    break;
                case "ExtensibleEntity":
                    bulkEntity= Client.RetrieveEntitiesBulk<ExtensibleEntity> ( );
                    break;
                case "BillableEvent":
                    bulkEntity= Client.RetrieveEntitiesBulk<BillableEvent> ( );
                    break;
                case "Product":
                    bulkEntity= Client.RetrieveEntitiesBulk<Product> ( );
                    break;
                case "UnitGroup":
                    bulkEntity= Client.RetrieveEntitiesBulk<UnitGroup> ( );
                    break;
                case "TaxProduct":
                    bulkEntity= Client.RetrieveEntitiesBulk<TaxProduct> ( );
                    break;
                case "SampleProduct":
                    bulkEntity= Client.RetrieveEntitiesBulk<SampleProduct> ( );
                    break;
                case "TerminationFeeRemainder":
                    bulkEntity= Client.RetrieveEntitiesBulk<TerminationFeeRemainder> ( );
                    break;
                case "TaxEvent":
                    bulkEntity= Client.RetrieveEntitiesBulk<TaxEvent> ( );
                    break;
                case "TerminationTimeServed":
                    bulkEntity= Client.RetrieveEntitiesBulk<TerminationTimeServed> ( );
                    break;
                case "ManualCharge":
                    bulkEntity= Client.RetrieveEntitiesBulk<ManualCharge> ( );
                    break;
                case "RecurringEvent":
                    bulkEntity= Client.RetrieveEntitiesBulk<RecurringEvent> ( );
                    break;
                case "ActivationFlat":
                    bulkEntity= Client.RetrieveEntitiesBulk<ActivationFlat> ( );
                    break;
                case "UsageEvent":
                    bulkEntity= Client.RetrieveEntitiesBulk<UsageEvent> ( );
                    break;
                case "TerminationFeeFlat":
                    bulkEntity= Client.RetrieveEntitiesBulk<TerminationFeeFlat> ( );
                    break;
                case "Package":
                    bulkEntity= Client.RetrieveEntitiesBulk<Package> ( );
                    break;
                case "SamplePackage":
                    bulkEntity= Client.RetrieveEntitiesBulk<SamplePackage> ( );
                    break;
                case "Subscription":
                    bulkEntity= Client.RetrieveEntitiesBulk<Subscription> ( );
                    break;
                case "Promotion":
                    bulkEntity= Client.RetrieveEntitiesBulk<Promotion> ( );
                    break;
                case "SampleSubscription":
                    bulkEntity= Client.RetrieveEntitiesBulk<SampleSubscription> ( );
                    break;
                case "Account":
                    bulkEntity= Client.RetrieveEntitiesBulk<Account> ( );
                    break;
                case "SampleAccount":
                    bulkEntity= Client.RetrieveEntitiesBulk<SampleAccount> ( );
                    break;
                case "SimpleAccountA":
                    bulkEntity= Client.RetrieveEntitiesBulk<SimpleAccountA> ( );
                    break;
                case "ManualPayment":
                    bulkEntity= Client.RetrieveEntitiesBulk<ManualPayment> ( );
                    break;
                case "Wire":
                    bulkEntity= Client.RetrieveEntitiesBulk<Wire> ( );
                    break;
                case "Check":
                    bulkEntity= Client.RetrieveEntitiesBulk<Check> ( );
                    break;
                case "ExternalBankAccount":
                    bulkEntity= Client.RetrieveEntitiesBulk<ExternalBankAccount> ( );
                    break;
                case "ExternalCreditCard":
                    bulkEntity= Client.RetrieveEntitiesBulk<ExternalCreditCard> ( );
                    break;
                case "Cash":
                    bulkEntity= Client.RetrieveEntitiesBulk<Cash> ( );
                    break;
                case "ElectronicPayment":
                    bulkEntity= Client.RetrieveEntitiesBulk<ElectronicPayment> ( );
                    break;
                case "Invoice":
                    bulkEntity= Client.RetrieveEntitiesBulk<Invoice> ( );
                    break;
                case "Notification":
                    bulkEntity= Client.RetrieveEntitiesBulk<Notification> ( );
                    break;
                case "NotificationEndpoint":
                    bulkEntity= Client.RetrieveEntitiesBulk<NotificationEndpoint> ( );
                    break;
                case "EntityType":
                    bulkEntity= Client.RetrieveEntitiesBulk<EntityType> ( );
                    break;
            }
            return bulkEntity;
        }
    }
}