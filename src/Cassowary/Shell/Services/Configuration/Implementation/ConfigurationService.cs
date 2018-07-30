using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xaml;
using System.Xml;
using Cassowary.Services.Configuration.Implementation.Serialization;

namespace Cassowary.Services.Configuration.Implementation
{
    [Export(typeof(IConfigurationService))]
    internal class ConfigurationService : IConfigurationService
    {
        private const string DefaultConfigResourceName = "Cassowary.Services.Configuration.Implementation.DefaultConfig.xml";

        #region MEF Imports

        [ImportMany(typeof(IConfigurationSection))]
        private IEnumerable<IConfigurationSection> ConfigurationSections { get; set; }

        #endregion

        #region Private Fields

        private ConfigurationRoot root;
        private ConfigurationSerializer serializer;

        #endregion

        internal ConfigurationService()
        {
            this.serializer = new ConfigurationSerializer();
        }

        #region IConfigurationService Implementation

        public string ConfigurationFileName { get; set; }        

        public void Load()
        {
            if (File.Exists(this.ConfigurationFileName))
            {
                try
                {
                    using (FileStream stream = File.OpenRead(this.ConfigurationFileName))
                    {
                        this.LoadFromStream(stream);
                    }
                }
                catch
                {
                    // TODO: Log failed load
                }
            }

            if (this.root == null)
            {
                Stream defaultConfigStream = this.GetType().Assembly.GetManifestResourceStream(ConfigurationService.DefaultConfigResourceName);
                this.LoadFromStream(defaultConfigStream);
            }
        }

        private void LoadFromStream(Stream stream)
        {
            using (XmlReader reader = XmlReader.Create(stream))
            {
                this.serializer.KnownTypes = this.ConfigurationSections.Select(s => s.SectionType);

                this.root = this.serializer.Read<ConfigurationRoot>(reader);

                // Populate sections
                foreach (object section in root.Sections)
                {
                    IConfigurationSection configSection = this.ConfigurationSections.FirstOrDefault(s => s.SectionType == section.GetType());
                    if (configSection != null)
                    {
                        configSection.Section = section;
                    }
                }
            }
        }

        public void Save()
        {
            XmlWriterSettings settings = new XmlWriterSettings()
            {
                Indent = true,
                Encoding = Encoding.UTF8,
            };

            using (FileStream stream = File.Open(this.ConfigurationFileName, FileMode.Create, FileAccess.Write))
            using (XmlWriter writer = XmlWriter.Create(stream, settings))
            {
                this.serializer.Write(writer, this.root);
            }
        }

        #endregion
    }
}
