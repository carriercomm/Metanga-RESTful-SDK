using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using CsvHelper;
using Metanga.Metadata;
using Metanga.SoftwareDevelopmentKit.Proxy;
using Metanga.SoftwareDevelopmentKit.Rest;

namespace MetangaImporter
{
    class Program
    {
        private static List<object> listObject = null;
        static MetadataClass metadata = new MetadataClass ( );
        private static string language;

        /// <summary>
        /// Mains the specified args.
        /// </summary>
        /// <param name="args">The args.</param>
        public static void Main ( string [] args )
        {
            language = args.Length < 1 ? "en-us" : args [ 0 ];

            Console.WriteLine ( "===========================" );
            Console.WriteLine ( "Metanga Importer" );
            Console.WriteLine ( "===========================" );
            Console.WriteLine ( "connection in progress..." );
            Console.WriteLine ( DateTime.Now.ToLocalTime ( ).ToString ( ) );

            metadata.Client = metadata.Metadata ( );
            IEnumerable<Type> entities = metadata.ReadAvailableEntities ( );
            Dictionary<int, Type> entityTable = new Dictionary<int, Type> ( );

            Console.WriteLine ( "connected..." );
            Console.WriteLine ( DateTime.Now.ToLocalTime ( ).ToString ( ) );
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
                    entityTable.Add ( count, entity );
                }

                Console.WriteLine ( "===========================" );
                Console.Write ( "Enter the number to select the available entity:" );
                string keyInfo = Console.ReadLine ( );

                int keyEntered;
                if ( int.TryParse ( keyInfo, out keyEntered ) )
                {
                    Type selectedEntityName;
                    entityTable.TryGetValue ( keyEntered, out selectedEntityName );
                    //if ( !string.IsNullOrEmpty ( entityName ) )
                    {
                        metadata.SelectedEntityName = selectedEntityName;
                        Console.WriteLine ( Environment.NewLine );
                        Console.WriteLine ( "Collecting Object Information..." );

                        try
                        {
                            IEnumerable<Entity> entitiesFromMetadata = metadata.ClientCallGetEntity ( );
                            listObject = metadata.CreateObject ( language, entitiesFromMetadata );
                            Console.WriteLine ( "Done" );

                            Console.WriteLine ( Environment.NewLine );
                            Console.WriteLine ( "Exporting it to CSV now..." );

                            WriteCsvFile ( selectedEntityName.Name );
                            Console.WriteLine ( "Done" );
                        }
                        catch ( MetangaException ex )
                        {
                            Console.WriteLine ( Environment.NewLine );
                            Console.WriteLine ( String.Format ( "An error has occurred: Id={0}, Message={1}", ex.ErrorId, ex.Message ) );
                        }


                    }
                }
                Console.WriteLine ( "Press any key to finish" );
                Console.ReadKey ( );
            }
        }

        /// <summary>
        /// Writes the CSV file.
        /// </summary>
        /// <param name="fileName">Name of the file.</param>
        public static void WriteCsvFile ( string fileName )
        {
            using ( var csv = new CsvWriter ( new StreamWriter ( string.Format ( "{0}_{1}.csv", fileName, DateTime.Now.ToOADate ( ).ToString ( ) ) ) ) )
            {
                var newProducts = Assembly.LoadFrom ( "MetangaDynamicAssembly.dll" ).CreateInstance ( "DynamicType" );
                dynamic newType = newProducts.GetType ( );
                csv.WriteHeader ( newType );
                foreach ( var item in listObject )
                {
                    foreach ( PropertyInfo entity in item.GetType ( ).GetProperties ( ) )
                    {
                        csv.WriteField ( entity.GetValue ( item, null ) );
                    }
                    csv.NextRecord ( );
                }
            }
        }
    }
}
