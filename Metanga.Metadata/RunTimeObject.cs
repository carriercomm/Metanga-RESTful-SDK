using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading;
using Metanga.SoftwareDevelopmentKit.Proxy;

namespace Metanga.Metadata
{
    public class RunTimeObject : IDisposable
    {

        void EmitGetter ( MethodBuilder methodBuilder, FieldBuilder fieldBuilder )
        {
            ILGenerator ilGenerator = methodBuilder.GetILGenerator ( );
            ilGenerator.Emit ( OpCodes.Ldarg_0 );
            ilGenerator.Emit ( OpCodes.Ldfld, fieldBuilder );
            ilGenerator.Emit ( OpCodes.Ret );
        }

        void EmitSetter ( MethodBuilder methodBuilder, FieldBuilder fieldBuilder )
        {
            ILGenerator ilGenerator = methodBuilder.GetILGenerator ( );
            ilGenerator.Emit ( OpCodes.Ldarg_0 );
            ilGenerator.Emit ( OpCodes.Ldarg_1 );
            ilGenerator.Emit ( OpCodes.Stfld, fieldBuilder );
            ilGenerator.Emit ( OpCodes.Ret );
        }

        public object CreateNewObject ( Entity obj, string languageCode, Type selectedEntity )
        {
            AssemblyName assemblyName = new AssemblyName { Name = "MetangaDynamicAssembly" };
            AssemblyBuilder assemblyBuilder = Thread.GetDomain ( ).DefineDynamicAssembly ( assemblyName, AssemblyBuilderAccess.RunAndSave );
            ModuleBuilder moduleBuilder = assemblyBuilder.DefineDynamicModule ( "module", assemblyName.Name+".dll" );
            TypeBuilder typeBuilder = moduleBuilder.DefineType ( "DynamicType", TypeAttributes.Public );
            //FieldBuilder fieldBuilder=typeBuilder.DefineField ( "intField", typeof ( int ), FieldAttributes.Public );
            int initialCount = 0;

            foreach ( PropertyInfo prop in obj.GetType ( ).GetProperties ( ) )
            {
                if ( ( ( prop.PropertyType.Name == "String" ) ||
                     ( prop.PropertyType.Name == "Int32" ) ||
                     ( prop.PropertyType.Name == "Boolean" ) ||
                     ( prop.PropertyType.Name == "Decimal" ) ) ||
                    ( prop.Name.Equals ( "Name" ) ) ||
                    ( prop.Name.Equals ( "Description" ) ) )
                {
                    initialCount++;
                    FieldBuilder field = typeBuilder.DefineField ( prop.Name, typeof ( string ), FieldAttributes.Public );
                    PropertyBuilder propertyBuilder = typeBuilder.DefineProperty ( prop.Name, PropertyAttributes.None, typeof ( string ), new Type [] { typeof ( string ) } );
                    MethodAttributes methodAttributes = MethodAttributes.Public | MethodAttributes.HideBySig;

                    MethodBuilder methodBuilderGetter = typeBuilder.DefineMethod ( "get_value", methodAttributes, typeof ( string ), Type.EmptyTypes );
                    EmitGetter ( methodBuilderGetter, field );

                    MethodBuilder methodBuilderSetter = typeBuilder.DefineMethod ( "set_value", methodAttributes, null, new Type [] { typeof ( string ) } );
                    EmitSetter ( methodBuilderSetter, field );

                    propertyBuilder.SetGetMethod ( methodBuilderGetter );
                    propertyBuilder.SetSetMethod ( methodBuilderSetter );
                }
            }

            Type dynamicType = typeBuilder.CreateType ( );

            var dynamicObject = Activator.CreateInstance ( dynamicType );

            var properties = dynamicType.GetProperties ( );

            int count = 0;

            foreach ( var item in obj.GetType ( ).GetProperties ( ) )
            {
                if ( count >= initialCount )
                    continue;
                if ( ( item.PropertyType.Name == "String" ) ||
                    ( item.PropertyType.Name == "Int32" ) ||
                    ( item.PropertyType.Name == "Boolean" ) ||
                    ( item.PropertyType.Name == "Decimal" ) )
                {

                    string itemValue = item.GetValue ( obj, null ) != null ? item.GetValue ( obj, null ).ToString ( ) : string.Empty;
                    properties [ count++ ].SetValue ( dynamicObject, itemValue, null );
                }
                //else if ( item.Name.Equals ( "ExternalId" ) )
                //{
                //    properties [ count++ ].SetValue ( dynamicObject, ( ( Product ) obj ).ExternalId [ "en-us" ], null );
                //}
                else if ( item.Name.Equals ( "Name" ) )
                {
                    try
                    {
                        properties [ count++ ].SetValue ( dynamicObject, ( obj ).Name [ languageCode ], null );
                    }
                    catch ( IndexOutOfRangeException )
                    {
                        properties [ count++ ].SetValue ( dynamicObject, ( obj ).Name, null );
                    }
                    catch ( KeyNotFoundException )
                    {
                        properties [ count++ ].SetValue ( dynamicObject, string.Empty, null );
                    }
                }
                else if ( item.Name.Equals ( "Description" ) )
                {
                    try
                    {
                        properties [ count++ ].SetValue ( dynamicObject, ( obj ).Description [ languageCode ], null );
                    }
                    catch ( KeyNotFoundException )
                    {
                        properties [ count++ ].SetValue ( dynamicObject, string.Empty, null );
                    }
                }
            }

            //assemblyBuilder = ( AssemblyBuilder ) dynamicType.Assembly;
            try
            {
                assemblyBuilder.Save ( assemblyName.Name+".dll" );
            }
            catch ( NotSupportedException ex )
            {
                throw ex;
            }
            catch ( InvalidOperationException ex )
            {

                throw ex;
            }

            return dynamicObject;

        }

        public void Dispose ( )
        {

        }
    }
}
