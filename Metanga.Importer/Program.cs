using System;
using System.CodeDom.Compiler;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using CommandLine.Text;
using CsvHelper;
using Metanga.Metadata;
using Metanga.SoftwareDevelopmentKit.Proxy;

namespace MetangaImporter
{
    class Program
    {
        public static void Main ( string [] args )
        {
            var metadata = new MetadataClass ( );
            metadata.Client = metadata.Metadata ( );
            IEnumerable<Type> entities = metadata.ReadAvailableEntities ( );
            Dictionary<int, string> entityTable = new Dictionary<int, string> ( );

            string fileName = @"C:\Sample";

            Console.WriteLine ( "===========================" );
            Console.WriteLine ( "Metanga Importer" );
            Console.WriteLine ( "===========================" );
            Console.WriteLine ( "Steps: " );
            Console.WriteLine ( "Step 1 to view available entities." );
            Console.WriteLine ( "Step 2 to download sample or upload formatted CSV File." );
            Console.WriteLine ( "===========================" );
            Console.Write ( "Press 1 now, to view available entities (anything else to exit.) :" );
            if ( Console.ReadLine ( ) == "1" )
            {
                Console.WriteLine ( "" );
                int counter = 0;
                foreach ( Type entity in entities )
                {
                    int count = counter++;
                    Console.WriteLine ( string.Format ( "{0}:{1}", count, entity.Name ) );
                    entityTable.Add ( count, entity.Name );
                }

                Console.WriteLine ( "===========================" );
                Console.Write ( "Enter the number to select the available entity:" );
                string keyInfo = Console.ReadLine ( );

                int keyEntered;
                if ( int.TryParse ( keyInfo, out keyEntered ) )
                {
                    string entityName;
                    entityTable.TryGetValue ( keyEntered, out entityName );
                    if ( !string.IsNullOrEmpty ( entityName.Trim ( ) ) )
                    {
                        IEnumerable<PropertyInfo> fields = metadata.ReadSelectedEntitysMethods ( entityName.Trim ( ) );
                        CreateSampleData ( entityName.Trim ( ), fields );
                        //if ( fields.Any ( ) )
                        //WriteCsvFile ( fileName, entityName.Trim ( ), fields );
                    }
                }
            }
        }

        /// <summary>
        /// Creates the sample data.
        /// </summary>
        /// <param name="fields">The fields.</param>
        /// <exception cref="System.NotImplementedException"></exception>
        private static void CreateSampleData ( string selectedFieldName, IEnumerable<PropertyInfo> fields )
        {
            DataTable dt = new DataTable ( selectedFieldName );
            int columnCount = 0;
            //var className = String.Format("Entity.{0}", fields.ScriptClass);
            PropertyInfo selectedField = null;

            foreach ( PropertyInfo propertyInfo in fields )
            {
                DataColumn dc = new DataColumn ( string.Format ( "{0}", columnCount++ ), propertyInfo.GetType ( ) );
                dt.Columns.Add ( dc );
            }

            //Activator.CreateInstance ( );
            //List<PropertyInfo> records
            for ( int rowCount = 0; rowCount < 2; rowCount++ )
            {
                foreach ( DataColumn column in dt.Columns )
                {
                    //propertyInfo.Name;
                    switch ( column.GetType ( ).ToString ( ) )
                    {
                        case "Boolean":

                            break;
                        case "String":
                            break;
                        case "EventType":
                            break;
                        case "ExtensionDataObject":
                            break;
                        case "Nullable'1":
                            break;
                        case "Dictionary'2":
                            break;
                        case "NotificationEndpointConfiguration":
                            break;
                        case "AuthenticationConfiguration":
                            break;
                        case "PaymentInstrumentMasked":
                            break;
                        case "PriceSchedule":
                            break;
                        case "ProductModel":
                            break;
                        case "DateTime":
                            break;
                        case "Int32":
                            break;
                        case "PackageProduct":
                            break;
                        case "PaymentInfoStatus":
                            break;
                        case "Package":
                            break;
                        case "Product":
                            break;
                        case "SubscriptionPackageProduct":
                            break;
                        case "ManualPaymentOperation":
                            break;
                        case "CreditCardType":
                            break;
                        case "ElectronicPaymentOperation":
                            break;
                        case "InvoiceState":
                            break;
                        case "EntityTypeColumn":
                            break;
                        default:
                            break;
                    }
                }
            }
            //new PropertyInfo
        }

        /// <summary>
        /// Writes the CSV file.
        /// </summary>
        /// <param name="fileName">Name of the file.</param>
        /// <param name="records">The records.</param>
        public static void WriteCsvFile ( string fileName, string entityName, IEnumerable<PropertyInfo> properties )
        {
            using ( var textWriter = File.CreateText ( string.Format ( "{0}_{1}.csv", fileName, entityName ) ) )
            using ( var writer = new CsvWriter ( textWriter ) )
            {
                List<PropertyInfo> records =  properties.ToList ( );
                foreach ( var record in records )
                {
                    //writer.WriteHeader<PropertyInfo> ( );
                    writer.WriteRecord ( record );
                }
            }
        }
    }
}
