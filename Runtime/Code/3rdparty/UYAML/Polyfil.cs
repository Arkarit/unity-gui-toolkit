using Codice.Client.BaseCommands;
using System;
using System.Collections.Generic;

namespace Lachee.UYAML
{
    #if !NET_STANDARD
    internal static class NetStandardPolyfill {
        public static bool TryPop<T>(this Stack<T> stack, out T result) {
            result = default;
            if (stack.Count == 0)
                return false;
            result = stack.Pop();
            return true;
        }

        public static bool TryAdd<T, K>(this Dictionary<T, K> dict, T key, K value) {
            if (dict.ContainsKey(key))
                return false;
            dict.Add(key, value);
            return true;
        }

        public static string[] Split(this string str, char separator, int count)
        {
            return str.Split(new char[] { separator }, count);
        }
    }
    #endif

    #if UNITY_5_3_OR_NEWER 
    public static class UExtensions {
        /// <summary>
        /// Replaces all fileIDs that match the given mapping
        /// </summary>
        /// <param name="property"></param>
        /// <param name="map"></param>
        public static void ReplaceFileID(this UProperty property, Dictionary<long, long> map) =>
            ForEachProperty(property, uProperty => ReplaceFileIdIfNecessary(uProperty, map));
 
        /// <summary>
        /// Replaces all GUIDs inside the map.
        /// </summary>
        /// <param name="property"></param>
        /// <param name="map"></param>
        public static void ReplaceGUID(this UProperty property, Dictionary<string, string> map) =>
            ForEachProperty(property, uProperty => ReplaceGUIDIfNecessary(uProperty, map));

        public static void ReplaceGUIDAndFileID(this UProperty property, Dictionary<string, string> mapGUID, Dictionary<long, long> mapID)
        {
	        ForEachProperty(property, uProperty =>
	        {
                ReplaceGUIDIfNecessary(uProperty, mapGUID);
		        ReplaceFileIdIfNecessary(uProperty, mapID);
	        });
        }

        /// <summary>
        /// Visit each property and perform callback on it
        /// </summary>
        /// <param name="property"></param>
        /// <param name="callback"></param>
        /// <exception cref="ArgumentNullException"></exception>
        public static void ForEachProperty(this UProperty property, Action<UProperty> callback) {
            if (callback == null)
                throw new ArgumentNullException(nameof(callback));

            if (property.value is UObject obj)
            {
                foreach (var kp in obj.properties.Values)
                    ForEachProperty(kp, callback);
                return;
            }

            if (property.value is UArray arr)
            {
                foreach (var item in arr.items)
                    ForEachProperty(new UProperty(string.Empty, item), callback);
                return;
            }

            callback.Invoke(property);
        }
 
        private static void ReplaceGUIDIfNecessary(UProperty property, Dictionary<string, string> map) {
	        if (property.name == "guid" && property.value is UValue value)
	        {
		        if (map.TryGetValue(value.value, out var dest)) 
			        value.value = dest.ToString();
	        }
        }

        private static void ReplaceFileIdIfNecessary(UProperty property, Dictionary<long, long> map) {
	        if (property.name == "fileID" && property.value is UValue value && long.TryParse(value.value, out var fileID))
	        {
		        if (map.TryGetValue(fileID, out var dest))
			        value.value = dest.ToString();
	        }
        }
   }
    #endif
}
