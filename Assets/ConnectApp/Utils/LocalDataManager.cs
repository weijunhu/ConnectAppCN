using System;
using UnityEngine;

namespace ConnectApp.Utils {
    public static class LocalDataManager {
        const string _leaderBoardUpdatedTimeKey = "leaderBoardUpdatedTimeKey";
        const string _tinyGameUrlKey = "tinyGameUrlKey";

        public static void saveTinyGameUrl(string url) {
            PlayerPrefs.SetString(key: _tinyGameUrlKey, value: url);
            PlayerPrefs.Save();
        }
        
        public static string getTinyGameUrl() {
            return PlayerPrefs.GetString(key: _tinyGameUrlKey) ?? "";
        }
        
        public static void markLeaderBoardUpdatedTime() {
            PlayerPrefs.SetString(key: _leaderBoardUpdatedTimeKey, DateTime.UtcNow.ToString("yyyy/MM/dd HH:mm:ss"));
            PlayerPrefs.Save();
        }

        public static bool needNoticeNewLeaderBoard(DateTime dateTime) {
            if (!PlayerPrefs.HasKey(key: _leaderBoardUpdatedTimeKey)) {
                // first check need notice
                return true;
            }

            var timeString = PlayerPrefs.GetString(key: _leaderBoardUpdatedTimeKey);
            DateTime.TryParse(s: timeString, out var newTime);
            return DateTime.Compare(t1: newTime, t2: dateTime) <= 0;
        }
    }
}