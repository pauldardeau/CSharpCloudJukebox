namespace CSharpCloudJukebox;

public class KeyValuePairs
{
   private readonly Dictionary<string, string> _keyValues;

   public KeyValuePairs()
   {
      _keyValues = new Dictionary<string, string>();
   }
   
   public void GetKeys(List<string> keys)
   {
      foreach (var key in _keyValues.Keys)
      {
         keys.Add(key);
      }
   }

   public bool HasKey(string key)
   {
      return _keyValues.ContainsKey(key);
   }

   public string GetValue(string key)
   {
      return _keyValues[key];
   }

   public void AddPair(string key, string value)
   {
      _keyValues.Add(key, value);
   }

   public bool RemovePair(string key)
   {
      if (_keyValues.ContainsKey(key))
      {
         return _keyValues.Remove(key);
      }
      else
      {
         return false;
      }
   }

   public void Clear()
   {
      _keyValues.Clear();
   }

   public int Size()
   {
      return _keyValues.Count;
   }

   public bool IsEmpty()
   {
      return _keyValues.Count == 0;
   }
}