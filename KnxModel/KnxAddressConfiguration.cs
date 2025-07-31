namespace KnxModel
{
    /// <summary>
    /// Configuration constants for KNX group addresses used throughout the system
    /// </summary>
    public static class KnxAddressConfiguration
    {
        #region Light Control Configuration
        
        /// <summary>
        /// Main group for lighting control (1)
        /// </summary>
        public const string LIGHTS_MAIN_GROUP = "1";
        
        /// <summary>
        /// Middle group for lighting control (1)
        /// </summary>
        public const string LIGHTS_MIDDLE_GROUP = "1";
        
        #endregion
        
        #region Shutter Control Configuration
        
        /// <summary>
        /// Main group for shutter control (4)
        /// </summary>
        public const string SHUTTERS_MAIN_GROUP = "4";
        
        /// <summary>
        /// Middle group for shutter movement control (UP/DOWN) (0)
        /// </summary>
        public const string SHUTTERS_MOVEMENT_MIDDLE_GROUP = "0";
        
        /// <summary>
        /// Middle group for shutter position control (absolute positioning) (2)
        /// </summary>
        public const string SHUTTERS_POSITION_MIDDLE_GROUP = "2";
        
        /// <summary>
        /// Middle group for shutter lock control (3)
        /// </summary>
        public const string SHUTTERS_LOCK_MIDDLE_GROUP = "3";
        
        /// <summary>
        /// Middle group for shutter stop/step control (1)
        /// </summary>
        public const string SHUTTERS_STOP_MIDDLE_GROUP = "1";
        
        /// <summary>
        /// Feedback offset for shutters - add this value to control sub group to get feedback sub group
        /// Control: 4/x/18 -> Feedback: 4/x/118
        /// </summary>
        public const int SHUTTER_FEEDBACK_OFFSET = 100;
        
        #endregion
        
        #region Address Helper Methods
        
        /// <summary>
        /// Creates a light control address
        /// </summary>
        /// <param name="subGroup">Sub group number (e.g., "01", "02", etc.)</param>
        /// <returns>Complete KNX address for light control</returns>
        public static string CreateLightControlAddress(string subGroup)
        {
            return $"{LIGHTS_MAIN_GROUP}/{LIGHTS_MIDDLE_GROUP}/{subGroup}";
        }
        
        /// <summary>
        /// Creates a light feedback address
        /// </summary>
        /// <param name="feedbackSubGroup">Feedback sub group number (e.g., "101", "102", etc.)</param>
        /// <returns>Complete KNX address for light feedback</returns>
        public static string CreateLightFeedbackAddress(string feedbackSubGroup)
        {
            return $"{LIGHTS_MAIN_GROUP}/{LIGHTS_MIDDLE_GROUP}/{feedbackSubGroup}";
        }
        
        /// <summary>
        /// Creates a shutter movement control address (UP/DOWN)
        /// </summary>
        /// <param name="subGroup">Sub group number (e.g., "1", "2", etc.)</param>
        /// <returns>Complete KNX address for shutter movement control</returns>
        public static string CreateShutterMovementAddress(string subGroup)
        {
            return $"{SHUTTERS_MAIN_GROUP}/{SHUTTERS_MOVEMENT_MIDDLE_GROUP}/{subGroup}";
        }
        
        /// <summary>
        /// Creates a shutter position control address (absolute positioning)
        /// </summary>
        /// <param name="subGroup">Sub group number (e.g., "1", "2", etc.)</param>
        /// <returns>Complete KNX address for shutter position control</returns>
        public static string CreateShutterPositionAddress(string subGroup)
        {
            return $"{SHUTTERS_MAIN_GROUP}/{SHUTTERS_POSITION_MIDDLE_GROUP}/{subGroup}";
        }
        
        /// <summary>
        /// Creates a shutter lock control address
        /// </summary>
        /// <param name="subGroup">Sub group number (e.g., "1", "2", etc.)</param>
        /// <returns>Complete KNX address for shutter lock control</returns>
        public static string CreateShutterLockAddress(string subGroup)
        {
            return $"{SHUTTERS_MAIN_GROUP}/{SHUTTERS_LOCK_MIDDLE_GROUP}/{subGroup}";
        }
        
        /// <summary>
        /// Creates a shutter stop/step control address
        /// </summary>
        /// <param name="subGroup">Sub group number (e.g., "1", "2", etc.)</param>
        /// <returns>Complete KNX address for shutter stop control</returns>
        public static string CreateShutterStopAddress(string subGroup)
        {
            return $"{SHUTTERS_MAIN_GROUP}/{SHUTTERS_STOP_MIDDLE_GROUP}/{subGroup}";
        }
        
        /// <summary>
        /// Creates a shutter movement feedback address
        /// </summary>
        /// <param name="controlSubGroup">Control sub group number</param>
        /// <returns>Complete KNX address for shutter movement feedback</returns>
        public static string CreateShutterMovementFeedbackAddress(string controlSubGroup)
        {
            var feedbackSubGroup = (int.Parse(controlSubGroup) + SHUTTER_FEEDBACK_OFFSET).ToString();
            return $"{SHUTTERS_MAIN_GROUP}/{SHUTTERS_MOVEMENT_MIDDLE_GROUP}/{feedbackSubGroup}";
        }
        
        /// <summary>
        /// Creates a shutter position feedback address
        /// </summary>
        /// <param name="controlSubGroup">Control sub group number</param>
        /// <returns>Complete KNX address for shutter position feedback</returns>
        public static string CreateShutterPositionFeedbackAddress(string controlSubGroup)
        {
            var feedbackSubGroup = (int.Parse(controlSubGroup) + SHUTTER_FEEDBACK_OFFSET).ToString();
            return $"{SHUTTERS_MAIN_GROUP}/{SHUTTERS_POSITION_MIDDLE_GROUP}/{feedbackSubGroup}";
        }
        
        /// <summary>
        /// Creates a shutter lock feedback address
        /// </summary>
        /// <param name="controlSubGroup">Control sub group number</param>
        /// <returns>Complete KNX address for shutter lock feedback</returns>
        public static string CreateShutterLockFeedbackAddress(string controlSubGroup)
        {
            var feedbackSubGroup = (int.Parse(controlSubGroup) + SHUTTER_FEEDBACK_OFFSET).ToString();
            return $"{SHUTTERS_MAIN_GROUP}/{SHUTTERS_LOCK_MIDDLE_GROUP}/{feedbackSubGroup}";
        }
        
        /// <summary>
        /// Creates a shutter stop/movement status feedback address
        /// </summary>
        /// <param name="controlSubGroup">Control sub group number</param>
        /// <returns>Complete KNX address for shutter movement status feedback</returns>
        public static string CreateShutterMovementStatusFeedbackAddress(string controlSubGroup)
        {
            var feedbackSubGroup = (int.Parse(controlSubGroup) + SHUTTER_FEEDBACK_OFFSET).ToString();
            return $"{SHUTTERS_MAIN_GROUP}/{SHUTTERS_STOP_MIDDLE_GROUP}/{feedbackSubGroup}";
        }
        
        #endregion
    }
}
