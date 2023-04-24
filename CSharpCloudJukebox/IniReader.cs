namespace CSharpCloudJukebox;

public class IniReader
{
   const string EolLf             = "\n";
   const string EolCr             = "\r";
   const string OpenBracket       = "[";
   const string CloseBracket      = "]";
   const string CommentIdentifier = "#";
   
   private string _iniFile;
   private string _fileContents;

   public IniReader(string iniFile)
   {
      _iniFile = iniFile;
      _fileContents = "";
   }

   public bool Read()
   {
      if (!ReadFile())
      {
         Console.WriteLine("error: unable to read configuration file: {0}",
                           _iniFile);
         return false;
      }

      return true;
   }

   public bool ReadSection(string section, KeyValuePairs mapSectionValues)
   {
      string sectionId = BracketedSection(section);
      int posSection = _fileContents.IndexOf(sectionId);
    
      if (posSection == -1)
      {
         return false;
      }
    
      int posEndSection = posSection + sectionId.Length;
      int startNextSection = _fileContents.IndexOf(OpenBracket, posEndSection);
    
      string sectionContents = "";
    
      // do we have another section?
      if (startNextSection != -1)
      {
         // yes, we have another section in the file -- read everything
         // up to the next section
         sectionContents = _fileContents.Substring(posEndSection,
            startNextSection - posEndSection);
      }
      else
      {
         // no, this is the last section -- read everything left in
         // the file
         sectionContents = _fileContents.Substring(posEndSection);
      }
    
      int posEol;
      int index = 0;
    
      // process each line of the section
      while ((posEol = sectionContents.IndexOf(EolLf, index)) != -1)
      {
         string line = sectionContents.Substring(index, posEol - index);
         if (line.Length > 0)
         {
            int posCR = line.IndexOf('\r');
            if (posCR != -1)
            {
               line = line.Substring(0, posCR);
            }
            
            int posEqual = line.IndexOf('=');
            
            if ((posEqual != -1) && (posEqual < line.Length))
            {
               string key = line.Substring(0, posEqual).Trim();
                
               // if the line is not a comment
               if (!key.StartsWith(CommentIdentifier))
               {
                  mapSectionValues.AddPair(key,
                     line.Substring(posEqual + 1).Trim());
               }
            }
         }
        
         index = posEol + 1;
      }
    
      return true;    
   }

   public bool GetSectionKeyValue(string section, string key, out string value)
   {
      KeyValuePairs map = new KeyValuePairs();
    
      if (!ReadSection(section, map))
      {
         Console.WriteLine("warning: IniReader ReadSection returned false");
         value = "";
         return false;
      }
    
      string strippedKey = key.Trim();
    
      if (!map.HasKey(strippedKey))
      {
         System.Console.WriteLine("debug: map does not contain key '{0}'",
                                  strippedKey);
         value = "";
         return false;
      }
    
      value = map.GetValue(key);
      return true;      
   }
   
   public bool HasSection(string section)
   {
      string sectionId = BracketedSection(section);
      return _fileContents.IndexOf(sectionId) > -1;
   }


   protected bool ReadFile()
   {
      if (!File.Exists(_iniFile))
      {
         return false;
      }
      
      _fileContents = File.ReadAllText(_iniFile);
   
      // strip out any comments
      bool strippingComments = true;
      int posCommentStart;
      int posCR;
      int posLF;
      int posEOL;
      int posCurrent = 0;
   
      while (strippingComments)
      {
         posCommentStart = _fileContents.IndexOf(CommentIdentifier, posCurrent);
         if (-1 == posCommentStart)
         {
            // not found
            strippingComments = false;
         }
         else
         {
            posCR = _fileContents.IndexOf(EolCr, posCommentStart + 1);
            posLF = _fileContents.IndexOf(EolLf, posCommentStart + 1);
            bool haveCR = (-1 != posCR);
            bool haveLF = (-1 != posLF);
         
            if (!haveCR && !haveLF)
            {
               // no end-of-line marker remaining
               // erase from start of comment to end of file
               _fileContents = _fileContents.Substring(0, posCommentStart);
               strippingComments = false;
            }
            else
            {
               // at least one end-of-line marker was found
            
               // were both types found
               if (haveCR && haveLF)
               {
                  posEOL = posCR;
               
                  if (posLF < posEOL)
                  {
                     posEOL = posLF;
                  }
               }
               else
               {
                  if (haveCR)
                  {
                     // CR found
                     posEOL = posCR;
                  }
                  else
                  {
                     // LF found
                     posEOL = posLF;
                  }
               }
            
               string beforeComment = _fileContents.Substring(0, posCommentStart);
               string afterComment = _fileContents.Substring(posEOL);
               _fileContents = beforeComment + afterComment;
               posCurrent = beforeComment.Length;
            }
         }
      }
   
      return true;   
   }
   
   protected string BracketedSection(string sectionName)
   {
      return OpenBracket + sectionName.Trim() + CloseBracket;
   }
}