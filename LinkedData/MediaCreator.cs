using Sitecore;
using Sitecore.Configuration;
using Sitecore.Data;
using Sitecore.Data.Fields;
using Sitecore.Data.Items;
using Sitecore.Diagnostics;
using Sitecore.Globalization;
using Sitecore.IO;
using Sitecore.Pipelines.GetMediaCreatorOptions;
using Sitecore.Resources.Media;
using Sitecore.SecurityModel;
using System;
using System.IO;

namespace LinkedData
{
    public class MediaCreator : Sitecore.Resources.Media.MediaCreator
    {
        /// <summary>
        /// The default site.
        /// 
        /// </summary>
        protected const string DefaultSite = "shell";

        /// <summary>
        /// Gets the file based stream path.
        /// 
        /// </summary>
        /// <param name="itemPath">The item path.</param><param name="filePath">The file path.</param><param name="options">The options.</param>
        /// <returns>
        /// The get file based stream path.
        /// </returns>
        public static string GetFileBasedStreamPath(string itemPath, string filePath, MediaCreatorOptions options)
        {
            Assert.ArgumentNotNull((object)itemPath, "itemPath");
            Assert.ArgumentNotNull((object)filePath, "filePath");
            Assert.ArgumentNotNull((object)options, "options");
            return MediaCreator.GetOutputFilePath(itemPath, filePath, options);
        }

        /// <summary>
        /// Attachs new stream to exists MediaItem and changes the FilePath
        /// 
        /// </summary>
        /// <param name="stream">The new file stream
        ///             </param><param name="itemPath">Full item patch
        ///             </param><param name="fileName">Filename with extension
        ///             </param><param name="options">The MediaCreator's Options
        ///             </param>
        /// <returns>
        /// The Media Item
        /// 
        /// </returns>
        public override Item AttachStreamToMediaItem(Stream stream, string itemPath, string fileName, MediaCreatorOptions options)
        {
            Assert.ArgumentNotNull((object)stream, "stream");
            Assert.ArgumentNotNullOrEmpty(fileName, "fileName");
            Assert.ArgumentNotNull((object)options, "options");
            Assert.ArgumentNotNull((object)itemPath, "itemPath");
            Media media = MediaManager.GetMedia((MediaItem)this.CreateItem(itemPath, fileName, options));
            media.SetStream(stream, FileUtil.GetExtension(fileName));
            return (Item)media.MediaData.MediaItem;
        }

        /// <summary>
        /// Creates a new media item from a file.
        /// 
        /// </summary>
        /// <param name="filePath">The file path.
        ///             </param><param name="options">The options.
        ///             </param>
        /// <returns>
        /// The Media Item.
        /// 
        /// </returns>
        public override MediaItem CreateFromFile(string filePath, MediaCreatorOptions options)
        {
            Assert.ArgumentNotNullOrEmpty(filePath, "filePath");
            Assert.ArgumentNotNull((object)options, "options");
            string path = FileUtil.MapPath(filePath);
            using (new SecurityDisabler())
            {
                using (FileStream fileStream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read))
                    return (MediaItem)this.CreateFromStream((Stream)fileStream, filePath, options);
            }
        }

        /// <summary>
        /// Creates a media folder from a file system folder.
        /// 
        /// </summary>
        /// <param name="folderPath">The folder path.
        ///             </param><param name="options">The options.
        ///             </param>
        /// <returns>
        /// The Sitecore item.
        /// 
        /// </returns>
        public override Item CreateFromFolder(string folderPath, MediaCreatorOptions options)
        {
            Assert.ArgumentNotNullOrEmpty(folderPath, "folderPath");
            Assert.ArgumentNotNull((object)options, "options");
            return this.CreateFolder(this.GetItemPath(folderPath, options), options);
        }

        /// <summary>
        /// Creates the media from a file.
        /// 
        /// </summary>
        /// <param name="stream">The stream.</param><param name="filePath">The file path.</param><param name="options">The options.</param>
        /// <returns>
        /// The Created Item.
        /// </returns>
        public override Item CreateFromStream(Stream stream, string filePath, MediaCreatorOptions options)
        {
            Assert.ArgumentNotNull((object)stream, "stream");
            Assert.ArgumentNotNull((object)filePath, "filePath");
            Assert.ArgumentNotNull((object)options, "options");
            return this.CreateFromStream(stream, filePath, true, options);
        }

        /// <summary>
        /// Creates the media from a file.
        /// 
        /// </summary>
        /// <param name="stream">The stream.
        ///             </param><param name="filePath">The file path.
        ///             </param><param name="setStreamIfEmpty">if set to <c>true</c> [set stream if empty].
        ///             </param><param name="options">The options.
        ///             </param>
        /// <returns>
        /// The Created Item.
        /// 
        /// </returns>
        public override Item CreateFromStream(Stream stream, string filePath, bool setStreamIfEmpty, MediaCreatorOptions options)
        {
            Assert.ArgumentNotNull((object)stream, "stream");
            Assert.ArgumentNotNullOrEmpty(filePath, "filePath");
            Assert.ArgumentNotNull((object)options, "options");
            string itemPath = this.GetItemPath(filePath, options);
            return this.AttachStreamToMediaItem(stream, itemPath, filePath, options);
        }

        /// <summary>
        /// A new file has been created.
        /// 
        /// </summary>
        /// <param name="filePath">The full path to the file.
        ///             </param>
        public override void FileCreated(string filePath)
        {
            Assert.ArgumentNotNullOrEmpty(filePath, "filePath");
            this.SetContext();
            lock (FileUtil.GetFileLock(filePath))
            {
                if (FileUtil.IsFolder(filePath))
                {
                    MediaCreatorOptions local_0 = MediaCreatorOptions.Empty;
                    local_0.Build(GetMediaCreatorOptionsArgs.FileBasedContext);
                    this.CreateFromFolder(filePath, local_0);
                }
                else
                {
                    MediaCreatorOptions local_1 = MediaCreatorOptions.Empty;
                    long local_2 = new FileInfo(filePath).Length;
                    local_1.FileBased = local_2 > Settings.Media.MaxSizeInDatabase || Settings.Media.UploadAsFiles;
                    local_1.Build(GetMediaCreatorOptionsArgs.FileBasedContext);
                    this.CreateFromFile(filePath, local_1);
                }
            }
        }

        /// <summary>
        /// A file has been deleted.
        /// 
        /// </summary>
        /// <param name="filePath">The full path to the file.
        ///             </param>
        public override void FileDeleted(string filePath)
        {
            Assert.ArgumentNotNullOrEmpty(filePath, "filePath");
        }

        /// <summary>
        /// A file has been renamed.
        /// 
        /// </summary>
        /// <param name="filePath">The path to the file.
        ///             </param><param name="oldFilePath">The old path to the file.
        ///             </param>
        public override void FileRenamed(string filePath, string oldFilePath)
        {
            Assert.ArgumentNotNullOrEmpty(filePath, "filePath");
            Assert.ArgumentNotNullOrEmpty(oldFilePath, "oldFilePath");
            this.SetContext();
            lock (FileUtil.GetFileLock(filePath))
            {
                MediaCreatorOptions local_0 = MediaCreatorOptions.Empty;
                local_0.Build(GetMediaCreatorOptionsArgs.FileBasedContext);
                string local_1 = this.GetItemPath(oldFilePath, local_0);
                Item local_3 = this.GetDatabase(local_0).GetItem(local_1);
                if (local_3 == null)
                    return;
                string local_5 = FileUtil.GetFileName(this.GetItemPath(filePath, local_0));
                string local_6 = FileUtil.GetExtension(filePath);
                using (new EditContext(local_3, SecurityCheck.Disable))
                {
                    local_3.Name = local_5;
                    local_3["extension"] = local_6;
                }
            }
        }

        /// <summary>
        /// Gets the item path corresponding to a file.
        /// 
        /// </summary>
        /// <param name="filePath">The file path.
        ///             </param><param name="options">The options.
        ///             </param>
        /// <returns>
        /// The item path.
        /// 
        /// </returns>
        public override string GetItemPath(string filePath, MediaCreatorOptions options)
        {
            Assert.ArgumentNotNull((object)filePath, "filePath");
            Assert.ArgumentNotNull((object)options, "options");
            if (!string.IsNullOrEmpty(options.Destination))
                return options.Destination;
            string text = FileUtil.SubtractPath(filePath, Settings.MediaFolder);
            Assert.IsNotNull((object)text, typeof(string), "File based media must be located beneath the media folder: '{0}'. Current file: {1}", (object)Settings.MediaFolder, (object)filePath);
            int length = text.LastIndexOf('.');
            if (length < text.LastIndexOf('\\'))
                length = -1;
            bool alwaysKeepExtension = FileUtil.IsFolder(filePath);
            if (length >= 0 && !alwaysKeepExtension)
            {
                string str = string.Empty;
                if (options.IncludeExtensionInItemName)
                    str = Settings.Media.WhitespaceReplacement + StringUtil.Mid(text, length + 1).ToLowerInvariant();
                text = StringUtil.Left(text, length) + str;
            }
            Assert.IsNotNullOrEmpty(text, "The relative path of a media to create is empty. Original file path: '{0}'.", (object)filePath);
            return Assert.ResultNotNull<string>(MediaPathManager.ProposeValidMediaPath(FileUtil.MakePath("/sitecore/media library", text.Replace('\\', '/')), alwaysKeepExtension));
        }

        /// <summary>
        /// Gets private folder for media blob inside the Media.FileFolder.
        /// 
        /// </summary>
        /// <param name="itemID">The item ID.
        ///             </param><param name="fullPath">The full path.
        ///             </param>
        /// <returns>
        /// The short folder.
        /// 
        /// </returns>
        public override string GetMediaStorageFolder(ID itemID, string fullPath)
        {
            Assert.IsNotNull((object)itemID, "itemID is null");
            Assert.IsNotNullOrEmpty(fullPath, "fullPath is empty");
            string fileName = FileUtil.GetFileName(fullPath);
            string str = itemID.ToString();
            return string.Format("/{0}/{1}/{2}/{3}{4}", (object)str[1], (object)str[2], (object)str[3], (object)str, (object)fileName);
        }

        /// <summary>
        /// Creates a media folder in the content database.
        /// 
        /// </summary>
        /// <param name="itemPath">The item path.
        ///             </param><param name="options">The options.
        ///             </param>
        /// <returns>
        /// The Created Folder.
        /// 
        /// </returns>
        protected override Item CreateFolder(string itemPath, MediaCreatorOptions options)
        {
            Assert.ArgumentNotNullOrEmpty(itemPath, "itemPath");
            Assert.ArgumentNotNull((object)options, "options");
            Item itemPath1;
            using (new SecurityDisabler())
            {
                TemplateItem folderTemplate = this.GetFolderTemplate(options);
                Database database = this.GetDatabase(options);
                Item obj = database.GetItem(itemPath, options.Language);
                if (obj != null)
                    return obj;
                itemPath1 = database.CreateItemPath(itemPath, folderTemplate, folderTemplate);
                Assert.IsNotNull((object)itemPath1, typeof(Item), "Could not create media folder: '{0}'.", (object)itemPath);
            }
            return itemPath1;
        }

        /// <summary>
        /// Creates a media item in the content database.
        /// 
        /// </summary>
        /// <param name="itemPath">The item path.
        ///             </param><param name="filePath">The file path.
        ///             </param><param name="options">The options.
        ///             </param>
        /// <returns>
        /// The Created Item.
        /// 
        /// </returns>
        protected override Item CreateItem(string itemPath, string filePath, MediaCreatorOptions options)
        {
            Assert.ArgumentNotNullOrEmpty(itemPath, "itemPath");
            Assert.ArgumentNotNullOrEmpty(filePath, "filePath");
            Assert.ArgumentNotNull((object)options, "options");
            Item obj1;
            using (new SecurityDisabler())
            {
                Database database = this.GetDatabase(options);
                Item obj2 = options.KeepExisting ? (Item)null : database.GetItem(itemPath, options.Language);
                Item parentFolder = this.GetParentFolder(itemPath, options);
                string itemName = this.GetItemName(itemPath);
                if (obj2 != null && !obj2.HasChildren && obj2.TemplateID != TemplateIDs.MediaFolder)
                {
                    obj1 = obj2;
                    obj1.Versions.RemoveAll(true);
                    obj1 = obj1.Database.GetItem(obj1.ID, obj1.Language, Sitecore.Data.Version.Latest);
                    Assert.IsNotNull((object)obj1, "item");
                    obj1.Editing.BeginEdit();
                    foreach (Field field in obj1.Fields)
                        field.Reset();
                    obj1.Editing.EndEdit();
                    obj1.Editing.BeginEdit();
                    obj1.Name = itemName;
                    obj1.TemplateID = this.GetItemTemplate(filePath, options).ID;
                    obj1.Editing.EndEdit();
                }
                else
                    obj1 = parentFolder.Add(itemName, this.GetItemTemplate(filePath, options));
                Assert.IsNotNull((object)obj1, typeof(Item), "Could not create media item: '{0}'.", (object)itemPath);
                Language[] languageArray1;
                if (!options.Versioned)
                    languageArray1 = obj1.Database.Languages;
                else
                    languageArray1 = new Language[1]
          {
            obj1.Language
          };
                Language[] languageArray2 = languageArray1;
                string extension = FileUtil.GetExtension(filePath);
                foreach (Language language in languageArray2)
                {
                    MediaItem mediaItem = (MediaItem)obj1.Database.GetItem(obj1.ID, language, Sitecore.Data.Version.Latest);
                    if (mediaItem != null)
                    {
                        using (new EditContext((Item)mediaItem, SecurityCheck.Disable))
                        {
                            mediaItem.Extension = StringUtil.GetString(new string[2]
              {
                mediaItem.Extension,
                extension
              });
                            mediaItem.FilePath = this.GetFullFilePath(obj1.ID, filePath, itemPath, options);
                            mediaItem.Alt = StringUtil.GetString(new string[2]
              {
                mediaItem.Alt,
                options.AlternateText
              });
                            mediaItem.InnerItem.Statistics.UpdateRevision();
                        }
                    }
                }
            }
            obj1.Reload();
            return obj1;
        }

        /// <summary>
        /// Creates full path for mediaItem to the Media storage
        /// 
        /// </summary>
        /// <param name="itemID">The Item's ID
        ///             </param><param name="fileName">The file path
        ///             </param><param name="itemPath">The item path
        ///             </param><param name="options">The options
        ///             </param>
        /// <returns>
        /// The full file path
        /// 
        /// </returns>
        protected override string GetFullFilePath(ID itemID, string fileName, string itemPath, MediaCreatorOptions options)
        {
            Assert.ArgumentNotNull((object)itemID, "itemID");
            Assert.ArgumentNotNull((object)fileName, "fileName");
            Assert.ArgumentNotNull((object)itemPath, "itemPath");
            Assert.ArgumentNotNull((object)options, "options");
            return MediaCreator.GetOutputFilePath(itemPath, this.GetMediaStorageFolder(itemID, fileName), options);
        }

        /// <summary>
        /// Gets the output file path.
        /// 
        /// </summary>
        /// <param name="itemPath">The item path.
        ///             </param><param name="filePath">The file path.
        ///             </param><param name="options">The options.
        ///             </param>
        /// <returns>
        /// The get output file path.
        /// 
        /// </returns>
        private static string GetOutputFilePath(string itemPath, string filePath, MediaCreatorOptions options)
        {
            Assert.ArgumentNotNull((object)itemPath, "itemPath");
            Assert.ArgumentNotNull((object)filePath, "filePath");
            Assert.ArgumentNotNull((object)options, "options");
            if (!options.FileBased)
                return string.Empty;
            if (!string.IsNullOrEmpty(options.OutputFilePath))
                return options.OutputFilePath;
            string extension = FileUtil.GetExtension(filePath);
            string str = FileUtil.GetFileName(filePath);
            if (extension.Length > 0)
                str = str.Substring(0, str.Length - extension.Length - 1);
            return MediaPathManager.GetMediaFilePath(string.Format("{0}/{1}", (object)FileUtil.GetParentPath(filePath), (object)str), extension);
        }

        /// <summary>
        /// Gets the database.
        /// 
        /// </summary>
        /// <param name="options">The options.
        ///             </param>
        /// <returns>
        /// The Database.
        /// 
        /// </returns>
        private Database GetDatabase(MediaCreatorOptions options)
        {
            Assert.ArgumentNotNull((object)options, "options");
            return Assert.ResultNotNull<Database>(options.Database ?? Context.ContentDatabase ?? Context.Database);
        }

        /// <summary>
        /// Gets the template for a media folder.
        /// 
        /// </summary>
        /// <param name="options">The options.
        ///             </param>
        /// <returns>
        /// The Template Item.
        /// 
        /// </returns>
        private TemplateItem GetFolderTemplate(MediaCreatorOptions options)
        {
            Assert.ArgumentNotNull((object)options, "options");
            TemplateItem templateItem = this.GetDatabase(options).Templates[TemplateIDs.MediaFolder];
            Assert.IsNotNull((object)templateItem, typeof(TemplateItem), "Could not find folder template for media. Template: '{0}'", (object)TemplateIDs.MediaFolder);
            return templateItem;
        }

        /// <summary>
        /// Gets the name of the item from a path.
        /// 
        /// </summary>
        /// <param name="itemPath">The item path.
        ///             </param>
        /// <returns>
        /// The get item name.
        /// 
        /// </returns>
        /// <exception cref="T:System.InvalidOperationException"><c>InvalidOperationException</c>.
        ///             </exception>
        private string GetItemName(string itemPath)
        {
            Assert.ArgumentNotNull((object)itemPath, "itemPath");
            string lastPart = StringUtil.GetLastPart(itemPath, '/', string.Empty);
            if (!string.IsNullOrEmpty(lastPart))
                return lastPart;
            if (!Settings.Media.IncludeExtensionsInItemNames)
                return "unnamed";
            throw new InvalidOperationException("Invalid item path for media item: " + itemPath);
        }

        /// <summary>
        /// Gets the template for a media item.
        /// 
        /// </summary>
        /// <param name="filePath">The file path of the media.
        ///             </param><param name="options">The options.
        ///             </param>
        /// <returns>
        /// The Template Item.
        /// 
        /// </returns>
        private TemplateItem GetItemTemplate(string filePath, MediaCreatorOptions options)
        {
            Assert.ArgumentNotNull((object)filePath, "filePath");
            Assert.ArgumentNotNull((object)options, "options");
            string extension = FileUtil.GetExtension(filePath);
            string template = MediaManager.Config.GetTemplate(extension, options.Versioned);
            Assert.IsNotNullOrEmpty(template, "Could not find template for extension '{0}' (versioned: {1}).", (object)extension, (object)(options.Versioned ? 1 : 0));
            TemplateItem templateItem = this.GetDatabase(options).Templates[template];
            Assert.IsNotNull((object)templateItem, typeof(TemplateItem), "Could not find item template for media. Template: '{0}'", (object)template);
            return templateItem;
        }

        /// <summary>
        /// Gets the parent folder from an item path.
        /// 
        /// </summary>
        /// <param name="itemPath">The item path.
        ///             </param><param name="options">The options.
        ///             </param>
        /// <returns>
        /// The parent folder.
        /// 
        /// </returns>
        private Item GetParentFolder(string itemPath, MediaCreatorOptions options)
        {
            Assert.ArgumentNotNull((object)itemPath, "itemPath");
            Assert.ArgumentNotNull((object)options, "options");
            string[] strArray = StringUtil.Divide(itemPath, '/', true);
            return this.CreateFolder(strArray.Length > 1 ? strArray[0] : "/sitecore/media library", options);
        }

        /// <summary>
        /// Sets the context (if it has not been set).
        /// 
        /// </summary>
        private void SetContext()
        {
            if (Context.Site != null)
                return;
            Context.SetActiveSite("shell");
        }
    }
}
