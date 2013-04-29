using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using Metanga.SoftwareDevelopmentKit.Proxy;
using Metanga.SoftwareDevelopmentKit.Rest;

namespace Metanga.Metadata
{
    /// <summary>
    /// a public class for metadata.
    /// </summary>
    public class MetadataClass
    {

        public MetangaClient Client { get; set; }
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
            //var nextTypes = Assembly.GetAssembly ( entityType ).GetTypes ( ).Where ( entityType.IsAssignableFrom ).ToList ( );
            //using ( FileStream fs = new FileStream ( @"c:\attributes.txt", FileMode.Create ) )
            //{
            //    using ( StreamWriter w = new StreamWriter ( fs, Encoding.UTF8 ) )
            //    {
            //        foreach ( Type nextType in nextTypes )
            //        {
            //            w.WriteLine ( "===========================" );
            //            w.WriteLine ( string.Format ( "{0}", nextType.Name ) );
            //            foreach ( PropertyInfo field in nextType.GetProperties ( ) )
            //            {
            //                w.WriteLine ( string.Format ( "{0} : {1}", field.Name, field.PropertyType.Name ) );
            //            }
            //        }
            //    }
            //}
            //Console.ReadKey ( );
            //return null;
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
            List<PropertyInfo> propertyArray = new List<PropertyInfo> ( );
            foreach ( PropertyInfo field in nextTypes.Where ( nextType => nextType.Name.Equals ( selectedEntity ) ).SelectMany ( nextType => nextType.GetProperties ( ) ) )
            {
                propertyArray.Add ( field );
                //Console.WriteLine ( string.Format ( "{0}", field.Name ) );
            }


            IEnumerable<PropertyInfo> properties =   nextTypes.Where ( nextType => nextType.Name.Equals ( selectedEntity ) ).SelectMany ( nextType => nextType.GetProperties ( ).ToList ( ) );
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
    }
}
