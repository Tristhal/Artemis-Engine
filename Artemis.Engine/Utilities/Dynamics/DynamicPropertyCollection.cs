﻿#region Using Statements

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

#endregion

namespace Artemis.Engine.Utilities.Dynamics
{

    /// <summary>
    /// An object that implements DynamicProperties. These are an extension of ordinary properties
    /// which behave similar to attributes on Python objects. In a DynamicPropertyCollection, properties
    /// can be set and retrieved at will, regardless of whether or not they were defined in the class, 
    /// and said properties are dynamically-typed.
    /// </summary>
    public class DynamicPropertyCollection
    {

        private static AttributeMemoService<DynamicPropertyCollection> attrMemoService
            = new AttributeMemoService<DynamicPropertyCollection>();

        static DynamicPropertyCollection()
        {
            var handler = new AttributeMemoService<DynamicPropertyCollection>
                             .AttributeHandler(t => t.SetupDynamicProperties());
            attrMemoService.RegisterHandler<HasDynamicPropertiesAttribute>(handler);
        }

        /// <summary>
        /// Setup the dynamic properties for this class.
        /// </summary>
        private void SetupDynamicProperties()
        {
            var type = GetType();
            var attribute = (HasDynamicPropertiesAttribute)Attribute.GetCustomAttribute(
                    type, typeof(HasDynamicPropertiesAttribute));

            if (attribute.HasDynamicPropertyList)
            {
                var properties = from name in attribute.DynamicPropertyNames
                                 select type.GetProperty(name);
                AddDynamicProperties(properties.ToArray(), exclusive: true);
            }
            else
            {
                AddDynamicProperties(type.GetProperties());
            }
        }

        /// <summary>
        /// Add all the dynamic properties attached to this object to this
        /// DynamicPropertyCollection.
        /// </summary>
        /// <param name="properties"></param>
        private void AddDynamicProperties(PropertyInfo[] properties, bool exclusive = false)
        {
            // If exclusive is true then it is expected that the properties suppled in the `properties`
            // array are exclusively those marked with a DynamicPropertyAttribute. 
            //
            // This allows us to throw an error if the user specifies an attribute as a DynamicProperty 
            // (in the DynamicPropertyList attribute of the HasDynamicProperties attribute) that isn't 
            // actually marked as a DynamicProperty.

            foreach (var property in properties)
            {
                if (exclusive || Reflection.HasAttribute<DynamicPropertyAttribute>(property))
                {
                    AddDynamicProperty(property);
                }
                else
                {
                    throw new DynamicPropertyException(
                        String.Format(
                            "The property with name '{0}' is not marked " +
                            "with a DynamicPropertyAttribute.", property.Name
                            )
                        );
                }
            }
        }

        /// <summary>
        /// Add a single DynamicProperty to this DynamicPropertyCollection.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="propertyInfo"></param>
        protected void AddDynamicProperty(PropertyInfo propertyInfo)
        {
            var name = propertyInfo.Name;

            var propGetter = propertyInfo.GetGetMethod();
            var propSetter = propertyInfo.GetSetMethod();

            Getter getter = () => propGetter.Invoke(this, null);
            Setter setter = (obj) => propSetter.Invoke(this, new object[] { obj });

            Define(name, getter, setter);
        }

        // The private dictionary of DynamicProperties.
        private Dictionary<string, IDynamicProperty> properties { get; set; }

        public DynamicPropertyCollection()
        {
            properties = new Dictionary<string, IDynamicProperty>();

            attrMemoService.Handle(this);
        }

        /// <summary>
        /// Define a dynamic property with the given name, getter, and setter.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="getter"></param>
        /// <param name="setter"></param>
        public void Define(
            string name, Getter getter, Setter setter, bool useInitialValue = false, object initialValue = null)
        {
            if (properties.ContainsKey(name))
            {
                throw new DynamicPropertyException(
                    String.Format(
                        "Object of type '{0}' ('{1}') already has a property with name '{2}'. Cannot redefine.",
                        GetType(), ToString(), name
                        )
                    );
            }
            else
            {
                properties.Add(name, new ComplexDynamicProperty(getter, setter, useInitialValue, initialValue));
            }
        }

        /// <summary>
        /// Define a dynamic property with the given name and getter.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="getter"></param>
        public void Define(
            string name, Getter getter, bool useInitialValue = false, object initialValue = null)
        {
            Define(name, getter, null, useInitialValue, initialValue);
        }

        /// <summary>
        /// Define a dynamic property with the given name and setter.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="setter"></param>
        public void Define(
            string name, Setter setter, bool useInitialValue = false, object initialValue = null)
        {
            Define(name, null, setter, useInitialValue, initialValue);
        }

        /// <summary>
        /// Set a dynamic property with the given name to the given value.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="val"></param>
        public void Set(string name, object val)
        {
            if (properties.ContainsKey(name))
            {
                properties[name].Value = val;
            }
            else
            {
                properties.Add(name, new SimpleDynamicProperty(val));
            }
        }

        /// <summary>
        /// Retrieve a value with the given name and cast it to T.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="name"></param>
        /// <param name="searchStatic">If true, this will attempt to return the ordinary property with the given name.</param>
        /// <param name="useDefault">If true, if no property with the given name is found the default object is returned.</param>
        /// <param name="def">The default object to return if `useDefault` is true.</param>
        /// <returns></returns>
        public T Get<T>(string name, bool searchStatic = false, bool useDefault = false, object def = null)
        {
            return (T)Get(name, searchStatic, useDefault, def);
        }

        /// <summary>
        /// Retrieve a value with the given name.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="searchStatic">If true, this will attempt to return the ordinary property with the given name.</param>
        /// <param name="useDefault">If true, if no property with the given name is found the default object is returned.</param>
        /// <param name="def">The default object to return if `useDefault` is true.</param>
        /// <returns></returns>
        public object Get(string name, bool searchStatic = false, bool useDefault = false, object def = null)
        {
            if (properties.ContainsKey(name))
            {
                return properties[name].Value;
            }
            if (searchStatic)
            {
                var staticProperty = GetType().GetProperty(name);
                if (staticProperty != null)
                {
                    return staticProperty.GetValue(this, null);
                }
            }
            if (useDefault)
            {
                return def;
            }
            throw new DynamicPropertyException(
                String.Format(
                    "Object of type '{0}' ('{1}') has no property '{2}'.",
                    GetType(), ToString(), name
                    )
                );
        }
    }
}
