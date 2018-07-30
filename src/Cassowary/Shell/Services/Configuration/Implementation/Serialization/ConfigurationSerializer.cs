using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Xml;

namespace Cassowary.Services.Configuration.Implementation.Serialization
{
    internal class ConfigurationSerializer
    {
        #region Private Fields

        private static readonly Type[] SimpleTypes = new Type[] { typeof(string), typeof(DateTime), typeof(Guid) };

        internal IEnumerable<Type> KnownTypes { get; set; }

        #endregion

        #region Serialization

        internal void Write(XmlWriter writer, Object obj)
        {
            string elementName = this.GetElementName(obj);
            writer.WriteStartElement(elementName);

            this.WriteProperties(writer, obj);

            writer.WriteEndElement();
        }

        private void WriteProperties(XmlWriter writer, Object obj)
        {
            Type objType = obj.GetType();

            string contentProperty = null;
            DefaultPropertyAttribute contentPropertyAttr = objType.GetCustomAttribute<DefaultPropertyAttribute>();
            if (contentPropertyAttr != null)
            {
                contentProperty = contentPropertyAttr.DefaultProperty;
            }

            List<PropertyInfo> complexProperties = new List<PropertyInfo>();

            foreach (PropertyInfo propertyInfo in objType.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            {
                if (propertyInfo.PropertyType.IsPrimitive || propertyInfo.PropertyType.IsEnum || ConfigurationSerializer.SimpleTypes.Contains(propertyInfo.PropertyType))
                {
                    writer.WriteAttributeString(propertyInfo.Name, propertyInfo.GetValue(obj).ToString());
                }
                else
                {
                    complexProperties.Add(propertyInfo);
                }
            }

            foreach (PropertyInfo propertyInfo in complexProperties)
            {
                string propertyName = this.GetPropertyName(propertyInfo);

                bool isContentProperty = String.Equals(propertyName, contentProperty, StringComparison.Ordinal);

                if (!isContentProperty)
                {
                    String propertyElementName = String.Format("{0}.{1}", this.GetElementName(obj), this.GetPropertyName(propertyInfo));
                    writer.WriteStartElement(propertyElementName);
                }

                if (typeof(IEnumerable).IsAssignableFrom(propertyInfo.PropertyType))
                {
                    IEnumerable items = (IEnumerable)propertyInfo.GetValue(obj);
                    foreach (Object item in items)
                    {
                        Write(writer, item);
                    }
                }
                else
                {
                    Write(writer, propertyInfo.GetValue(obj));
                }

                if (!isContentProperty)
                {
                    writer.WriteEndElement();
                }
            }
        }

        #endregion

        #region Deserialization

        internal T Read<T>(XmlReader reader)
        {
            reader.MoveToContent();

            string elementName = this.GetElementName(typeof(T));

            if (!String.Equals(reader.Name, elementName, StringComparison.Ordinal))
            {
                throw new InvalidOperationException(String.Format(CultureInfo.CurrentCulture,
                    "Unexpected node type '{0}', expected '{1}'!", reader.Name, elementName));
            }

            return (T)ReadObject(reader, typeof(T));
        }

        private object ReadObject(XmlReader reader, Type objType)
        {
            Object obj = Activator.CreateInstance(objType, true);

            bool hasContent = !reader.IsEmptyElement;

            this.ReadSimpleProperties(reader, obj);

            reader.ReadStartElement();
            reader.MoveToContent();

            // If the element is empty, we're done
            if (!hasContent)
            {
                return obj;
            }

            this.ReadComplexProperties(reader, obj);

            reader.ReadEndElement();

            return obj;
        }

        private void ReadSimpleProperties(XmlReader reader, object obj)
        {
            Type objType = obj.GetType();

            while (reader.MoveToNextAttribute())
            {
                PropertyInfo simpleProperty = objType.GetProperty(reader.Name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (simpleProperty == null)
                {
                    throw new InvalidOperationException(String.Format(CultureInfo.InvariantCulture,
                        "Type '{0}' does not contain property '{1}'!", objType.Name, reader.Name));
                }

                this.ReadSimpleProperty(reader, obj, simpleProperty);
            }
        }

        private void ReadSimpleProperty(XmlReader reader, object obj, PropertyInfo simpleProperty)
        {
            object value = null;

            if (simpleProperty.PropertyType.IsPrimitive)
            {
                value = Convert.ChangeType(reader.Value, simpleProperty.PropertyType);
            }
            else if (simpleProperty.PropertyType == typeof(string))
            {
                value = reader.Value;
            }
            else if (simpleProperty.PropertyType.IsEnum)
            {
                value = Enum.Parse(simpleProperty.PropertyType, reader.Value);
            }
            else if (simpleProperty.PropertyType == typeof(DateTime))
            {
                value = DateTime.Parse(reader.Value);
            }
            else if (simpleProperty.PropertyType == typeof(Guid))
            {
                value = Guid.Parse(reader.Value);
            }

            simpleProperty.SetValue(obj, value);
        }

        private void ReadComplexProperties(XmlReader reader, object obj)
        {
            Type objType = obj.GetType();
            string elementName = this.GetElementName(objType);

            while (reader.NodeType != XmlNodeType.EndElement)
            {
                bool isExplicitProperty;
                PropertyInfo complexProperty = GetComplexProperty(reader, objType, out isExplicitProperty);

                if (complexProperty == null)
                {
                    throw new InvalidOperationException(String.Format(CultureInfo.InvariantCulture,
                        "Type '{0}' does not contain property '{1}'!", objType.Name, reader.Name));
                }

                this.ReadComplexProperty(reader, obj, complexProperty);

                if (isExplicitProperty)
                {
                    reader.ReadEndElement();
                }

                reader.MoveToContent();
            }
        }

        private PropertyInfo GetComplexProperty(XmlReader reader, Type objType, out bool isExplicitProperty)
        {
            string propertyName = this.GetDefaultProperty(objType);
            string elementName = this.GetElementName(objType);

            isExplicitProperty = false;

            if (reader.Name.IndexOf('.') != -1)
            {
                if (String.IsNullOrEmpty(propertyName))
                {
                    throw new InvalidOperationException(String.Format(CultureInfo.CurrentCulture,
                        "Unexpected property node '{0}'!", reader.Name));
                }

                string[] parts = reader.Name.Split('.');
                if (parts.Length != 2 || !String.Equals(parts[0], elementName, StringComparison.Ordinal))
                {
                    throw new InvalidOperationException(String.Format(CultureInfo.CurrentCulture,
                        "Unexpected property node '{0}'!", reader.Name));
                }

                propertyName = parts[1];
                isExplicitProperty = true;
            }

            return objType.GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        }

        private void ReadComplexProperty(XmlReader reader, object obj, PropertyInfo complexProperty)
        {
            Type collectionInterface = complexProperty.PropertyType.GetInterfaces().FirstOrDefault(i => i.GetGenericTypeDefinition() == typeof(ICollection<>));
            if (collectionInterface != null)
            {
                // Property is a collection
                Type collectionType = collectionInterface.GetGenericArguments().First();
                Type propertyType = GetPropertyType(reader, complexProperty, collectionType);

                // Make sure the type is valid for the collection
                if (!collectionType.IsAssignableFrom(propertyType))
                {
                    throw new InvalidOperationException(String.Format(CultureInfo.CurrentCulture,
                        "Element of type '{0}' cannot be added to collection '{1}' of type '{2}'!", propertyType.Name, complexProperty.Name, collectionType.Name));
                }

                object newElement = ReadObject(reader, propertyType);

                Object collection = complexProperty.GetValue(obj);

                MethodInfo addMethod = collection.GetType().GetMethod("Add", new Type[] { collectionType });
                addMethod.Invoke(collection, new object[] { newElement });
            }
            else
            {
                // Property is a scalar
                Type propertyType = GetPropertyType(reader, complexProperty, complexProperty.PropertyType);

                object newElement = ReadObject(reader, propertyType);

                complexProperty.SetValue(obj, newElement);
            }
        }

        private Type GetPropertyType(XmlReader reader, PropertyInfo complexProperty, Type propertyType)
        {
            string propertyElementName = this.GetElementName(propertyType);

            if (!String.Equals(propertyElementName, reader.Name, StringComparison.Ordinal))
            {
                // Element type doesn't match the type of the property - first, look for a 
                //  CollectionMemberType attribute
                propertyType = complexProperty.GetCustomAttributes<ValidTypeAttribute>()
                    .Where(a => String.Equals(reader.Name, this.GetElementName(a.ValidType), StringComparison.Ordinal))
                    .Select(a => a.ValidType)
                    .FirstOrDefault();

                if (propertyType == null)
                {
                    // No attribute, check the list passed in
                    if (this.KnownTypes != null)
                    {
                        propertyType = this.KnownTypes.FirstOrDefault(t => String.Equals(reader.Name, this.GetElementName(t), StringComparison.Ordinal));
                    }

                    if (propertyType == null)
                    {
                        throw new InvalidOperationException(String.Format(CultureInfo.CurrentCulture,
                           "Unexpected type '{0}' for property '{1}', expected '{2}'!", reader.Name, complexProperty.Name, propertyElementName));
                    }
                }
            }

            return propertyType;
        }

        #endregion

        #region Utilities

        internal string GetElementName(object obj)
        {
            Type objType = obj.GetType();

            return GetElementName(objType);
        }

        internal string GetElementName(Type objType)
        {
            SerializedNameAttribute nameAttr = objType.GetCustomAttribute<SerializedNameAttribute>();
            if (nameAttr != null)
            {
                return nameAttr.Name;
            }

            return objType.Name;
        }

        internal string GetPropertyName(PropertyInfo propertyInfo)
        {
            SerializedNameAttribute nameAttr = propertyInfo.GetCustomAttribute<SerializedNameAttribute>();
            if (nameAttr != null)
            {
                return nameAttr.Name;
            }

            return propertyInfo.Name;
        }

        private string GetDefaultProperty(Type objType)
        {
            DefaultPropertyAttribute contentPropertyAttr = objType.GetCustomAttribute<DefaultPropertyAttribute>();
            if (contentPropertyAttr != null)
            {
                return contentPropertyAttr.DefaultProperty;
            }

            return null;
        }

        #endregion
    }
}
