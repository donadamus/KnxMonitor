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
        
        /// <summary>
        /// Middle group for lighting lock control (2)
        /// </summary>
        public const string LIGHTS_LOCK_MIDDLE_GROUP = "2";

        /// <summary>
        /// Feedback offset for lights - add this value to control sub group to get feedback sub group
        /// Control: 1/x/11 -> Feedback: 1/x/111
        /// </summary>
        public const int LIGHT_FEEDBACK_OFFSET = 100;

        /// <summary>
        /// Feedback offset for light locks - set to 0 because switch doesn't send automatic feedback
        /// We treat write messages as feedback since lock uses the same address for control and status
        /// Control: 1/2/11 -> Feedback: 1/2/11 (same address)
        /// </summary>
        public const int LIGHT_LOCK_FEEDBACK_OFFSET = 0;

        
        #endregion

        #region Dimmer Control Configuration
        
        /// <summary>
        /// Main group for dimmer control (2)
        /// </summary>
        public const string DIMMERS_MAIN_GROUP = "2";
        
        /// <summary>
        /// Middle group for dimmer switch control (on/off) (1)
        /// </summary>
        public const string DIMMERS_SWITCH_MIDDLE_GROUP = "1";
        
        /// <summary>
        /// Middle group for dimmer brightness control (0-100%) (2)
        /// </summary>
        public const string DIMMERS_BRIGHTNESS_MIDDLE_GROUP = "2";
        
        /// <summary>
        /// Middle group for dimmer lock control (3)
        /// </summary>
        public const string DIMMERS_LOCK_MIDDLE_GROUP = "3";

        /// <summary>
        /// Feedback offset for dimmers - add this value to control sub group to get feedback sub group
        /// Control: 2/x/1 -> Feedback: 2/x/101
        /// </summary>
        public const int DIMMER_FEEDBACK_OFFSET = 100;

        /// <summary>
        /// Feedback offset for light locks - set to 0 because switch doesn't send automatic feedback
        /// We treat write messages as feedback since lock uses the same address for control and status
        /// Control: 1/2/11 -> Feedback: 1/2/11 (same address)
        /// </summary>
        public const int DIMMER_LOCK_FEEDBACK_OFFSET = 0;

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
        /// Middle group for shutter lock control (3)
        /// </summary>
        public const string SHUTTERS_SUN_PROTECTION_BLOCK_MIDDLE_GROUP = "4";

        /// <summary>
        /// Middle group for shutter sun protection status (4) - receives current state with +100 offset
        /// </summary>
        public const string SHUTTERS_SUN_PROTECTION_STATUS_MIDDLE_GROUP = "4";
        
        /// <summary>
        /// Middle group for shutter stop/step control (1)
        /// </summary>
        public const string SHUTTERS_STOP_MIDDLE_GROUP = "1";
        
        /// <summary>
        /// Feedback offset for shutters - add this value to control sub group to get feedback sub group
        /// Control: 4/x/18 -> Feedback: 4/x/118
        /// </summary>
        public const int SHUTTER_FEEDBACK_OFFSET = 100;

        /// <summary>
        /// Feedback offset for shutter sun protection block - set to 0 because switch doesn't send automatic feedback
        /// We treat write messages as feedback since sun protection block uses the same address for control and status
        /// Control: 4/4/1 -> Feedback: 4/4/1 (same address)
        /// </summary>
        public const int SHUTTER_SUN_PROTECTION_BLOCK_FEEDBACK_OFFSET = 0;
        
        #endregion

        #region Clock Control Configuration

        /// <summary>
        /// Main group for clock synchronization (0)
        /// </summary>
        public const string CLOCK_MAIN_GROUP = "0";

        /// <summary>
        /// Middle group for clock time synchronization (0)
        /// </summary>
        public const string CLOCK_TIME_MIDDLE_GROUP = "0";

        /// <summary>
        /// Sub group for clock time synchronization (1)
        /// </summary>
        public const string CLOCK_TIME_SUB_GROUP = "1";

        #endregion

        #region Sun Protection Threshold Configuration

        /// <summary>
        /// Main group for environmental sensors and thresholds (0)
        /// </summary>
        public const string ENVIRONMENT_MAIN_GROUP = "0";

        /// <summary>
        /// Middle group for threshold feedback (2)
        /// </summary>
        public const string THRESHOLD_MIDDLE_GROUP = "2";

        /// <summary>
        /// Sub group for brightness threshold 1 (3)
        /// </summary>
        public const string BRIGHTNESS_THRESHOLD_1_SUB_GROUP = "3";

        /// <summary>
        /// Sub group for brightness threshold 2 (4)
        /// </summary>
        public const string BRIGHTNESS_THRESHOLD_2_SUB_GROUP = "4";

        /// <summary>
        /// Sub group for outdoor temperature threshold (8)
        /// </summary>
        public const string OUTDOOR_TEMPERATURE_THRESHOLD_SUB_GROUP = "8";

        /// <summary>
        /// Sub group for brightness threshold monitoring device block control (12)
        /// </summary>
        public const string BRIGHTNESS_THRESHOLD_MONITORING_BLOCK_SUB_GROUP = "12";

        #endregion
        
        #region Sun Protection Threshold Address Creation Methods
        
        /// <summary>
        /// Creates brightness threshold 1 address
        /// </summary>
        /// <returns>Complete KNX address for brightness threshold 1</returns>
        public static string CreateBrightnessThreshold1Address()
        {
            return $"{ENVIRONMENT_MAIN_GROUP}/{THRESHOLD_MIDDLE_GROUP}/{BRIGHTNESS_THRESHOLD_1_SUB_GROUP}";
        }
        
        /// <summary>
        /// Creates brightness threshold 2 address
        /// </summary>
        /// <returns>Complete KNX address for brightness threshold 2</returns>
        public static string CreateBrightnessThreshold2Address()
        {
            return $"{ENVIRONMENT_MAIN_GROUP}/{THRESHOLD_MIDDLE_GROUP}/{BRIGHTNESS_THRESHOLD_2_SUB_GROUP}";
        }
        
        /// <summary>
        /// Creates outdoor temperature threshold address
        /// </summary>
        /// <returns>Complete KNX address for outdoor temperature threshold</returns>
        public static string CreateOutdoorTemperatureThresholdAddress()
        {
            return $"{ENVIRONMENT_MAIN_GROUP}/{THRESHOLD_MIDDLE_GROUP}/{OUTDOOR_TEMPERATURE_THRESHOLD_SUB_GROUP}";
        }
        
        /// <summary>
        /// Creates brightness threshold monitoring device block control address
        /// </summary>
        /// <returns>Complete KNX address for brightness threshold monitoring device block control</returns>
        public static string CreateBrightnessThresholdMonitoringBlockAddress()
        {
            return $"{ENVIRONMENT_MAIN_GROUP}/{THRESHOLD_MIDDLE_GROUP}/{BRIGHTNESS_THRESHOLD_MONITORING_BLOCK_SUB_GROUP}";
        }
        
        #endregion

        #region Dimmer Address Creation Methods
        
        /// <summary>
        /// Creates a dimmer switch control address (on/off)
        /// </summary>
        /// <param name="subGroup">Sub group number (e.g., "1", "2", etc.)</param>
        /// <returns>Complete KNX address for dimmer switch control</returns>
        public static string CreateDimmerSwitchControlAddress(string subGroup)
        {
            return $"{DIMMERS_MAIN_GROUP}/{DIMMERS_SWITCH_MIDDLE_GROUP}/{subGroup}";
        }
        
        /// <summary>
        /// Creates a dimmer switch feedback address (on/off)
        /// </summary>
        /// <param name="controlSubGroup">Control sub group number (e.g., "1", "2", etc.)</param>
        /// <returns>Complete KNX address for dimmer switch feedback</returns>
        public static string CreateDimmerSwitchFeedbackAddress(string controlSubGroup)
        {
            var feedbackSubGroup = (int.Parse(controlSubGroup) + DIMMER_FEEDBACK_OFFSET).ToString();
            return $"{DIMMERS_MAIN_GROUP}/{DIMMERS_SWITCH_MIDDLE_GROUP}/{feedbackSubGroup}";
        }
        
        /// <summary>
        /// Creates a dimmer brightness control address (0-100%)
        /// </summary>
        /// <param name="subGroup">Sub group number (e.g., "1", "2", etc.)</param>
        /// <returns>Complete KNX address for dimmer brightness control</returns>
        public static string CreateDimmerBrightnessControlAddress(string subGroup)
        {
            return $"{DIMMERS_MAIN_GROUP}/{DIMMERS_BRIGHTNESS_MIDDLE_GROUP}/{subGroup}";
        }
        
        /// <summary>
        /// Creates a dimmer brightness feedback address (0-100%)
        /// </summary>
        /// <param name="controlSubGroup">Control sub group number (e.g., "1", "2", etc.)</param>
        /// <returns>Complete KNX address for dimmer brightness feedback</returns>
        public static string CreateDimmerBrightnessFeedbackAddress(string controlSubGroup)
        {
            var feedbackSubGroup = (int.Parse(controlSubGroup) + DIMMER_FEEDBACK_OFFSET).ToString();
            return $"{DIMMERS_MAIN_GROUP}/{DIMMERS_BRIGHTNESS_MIDDLE_GROUP}/{feedbackSubGroup}";
        }
        
        /// <summary>
        /// Creates a dimmer lock control address
        /// </summary>
        /// <param name="subGroup">Sub group number (e.g., "1", "2", etc.)</param>
        /// <returns>Complete KNX address for dimmer lock control</returns>
        public static string CreateDimmerLockAddress(string subGroup)
        {
            return $"{DIMMERS_MAIN_GROUP}/{DIMMERS_LOCK_MIDDLE_GROUP}/{subGroup}";
        }
        
        /// <summary>
        /// Creates a dimmer lock feedback address
        /// </summary>
        /// <param name="controlSubGroup">Control sub group number (e.g., "1", "2", etc.)</param>
        /// <returns>Complete KNX address for dimmer lock feedback</returns>
        public static string CreateDimmerLockFeedbackAddress(string controlSubGroup)
        {
            var feedbackSubGroup = (int.Parse(controlSubGroup) + DIMMER_LOCK_FEEDBACK_OFFSET).ToString();
            return $"{DIMMERS_MAIN_GROUP}/{DIMMERS_LOCK_MIDDLE_GROUP}/{feedbackSubGroup}";
        }
        
        #endregion
        
        #region Address Helper Methods
        
        /// <summary>
        /// Creates a light control address
        /// </summary>
        /// <param name="subGroup">Sub group number (e.g., "01", "02", etc.)</param>
        /// <summary>
        /// Creates a light control address
        /// </summary>
        /// <param name="subGroup">Sub group number (e.g., "11", "12", etc.)</param>
        /// <returns>Complete KNX address for light control</returns>
        public static string CreateLightControlAddress(string subGroup)
        {
            return $"{LIGHTS_MAIN_GROUP}/{LIGHTS_MIDDLE_GROUP}/{subGroup}";
        }
        
        /// <summary>
        /// Creates a light feedback address
        /// </summary>
        /// <param name="controlSubGroup">Control sub group number (e.g., "11", "12", etc.)</param>
        /// <returns>Complete KNX address for light feedback</returns>
        public static string CreateLightFeedbackAddress(string controlSubGroup)
        {
            var feedbackSubGroup = (int.Parse(controlSubGroup) + LIGHT_FEEDBACK_OFFSET).ToString();
            return $"{LIGHTS_MAIN_GROUP}/{LIGHTS_MIDDLE_GROUP}/{feedbackSubGroup}";
        }
        
        /// <summary>
        /// Creates a light lock control address
        /// </summary>
        /// <param name="subGroup">Sub group number (e.g., "11", "12", etc.)</param>
        /// <returns>Complete KNX address for light lock control</returns>
        public static string CreateLightLockAddress(string subGroup)
        {
            return $"{LIGHTS_MAIN_GROUP}/{LIGHTS_LOCK_MIDDLE_GROUP}/{subGroup}";
        }
        
        /// <summary>
        /// Creates a light lock feedback address
        /// </summary>
        /// <param name="controlSubGroup">Control sub group number (e.g., "11", "12", etc.)</param>
        /// <returns>Complete KNX address for light lock feedback</returns>
        public static string CreateLightLockFeedbackAddress(string controlSubGroup)
        {
            var feedbackSubGroup = (int.Parse(controlSubGroup) + LIGHT_LOCK_FEEDBACK_OFFSET).ToString();
            return $"{LIGHTS_MAIN_GROUP}/{LIGHTS_LOCK_MIDDLE_GROUP}/{feedbackSubGroup}";
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

        /// <summary>
        /// Creates a shutter sun protection status address (current state with +100 offset)
        /// </summary>
        /// <param name="subGroup">Sub group number</param>
        /// <returns>Complete KNX address for shutter sun protection status</returns>
        public static string CreateShutterSunProtectionActiveAddress(string subGroup)
        {
            var statusSubGroup = (int.Parse(subGroup) + SHUTTER_FEEDBACK_OFFSET).ToString();
            return $"{SHUTTERS_MAIN_GROUP}/{SHUTTERS_SUN_PROTECTION_STATUS_MIDDLE_GROUP}/{statusSubGroup}";
        }

        /// <summary>
        /// Creates a clock time synchronization address
        /// </summary>
        /// <returns>Complete KNX address for clock time synchronization (fixed at 0/0/1)</returns>
        public static string CreateClockTimeAddress()
        {
            return $"{CLOCK_MAIN_GROUP}/{CLOCK_TIME_MIDDLE_GROUP}/{CLOCK_TIME_SUB_GROUP}";
        }

        #endregion

        #region Address Factory Methods

        /// <summary>
        /// Creates a complete LightAddresses object for a light device
        /// </summary>
        /// <param name="subGroup">Sub group number (e.g., "1", "2", etc.)</param>
        /// <returns>Complete LightAddresses object</returns>
        public static LightAddresses CreateLightAddresses(string subGroup)
        {
            return new LightAddresses(
                Control: CreateLightControlAddress(subGroup),
                Feedback: CreateLightFeedbackAddress(subGroup),
                LockControl: CreateLightLockAddress(subGroup),
                LockFeedback: CreateLightLockFeedbackAddress(subGroup)
            );
        }

        /// <summary>
        /// Creates a complete ShutterAddresses object for a shutter device
        /// </summary>
        /// <param name="subGroup">Sub group number (e.g., "1", "2", etc.)</param>
        /// <returns>Complete ShutterAddresses object</returns>
        public static ShutterAddresses CreateShutterAddresses(string subGroup)
        {
            return new ShutterAddresses(
                MovementControl: CreateShutterMovementAddress(subGroup),
                MovementFeedback: CreateShutterMovementFeedbackAddress(subGroup),
                PercentageControl: CreateShutterPositionAddress(subGroup),
                PercentageFeedback: CreateShutterPositionFeedbackAddress(subGroup),
                LockControl: CreateShutterLockAddress(subGroup),
                LockFeedback: CreateShutterLockFeedbackAddress(subGroup),
                SunProtectionBlockControl: CreateShutterSunProtectionBlockAddress(subGroup),
                SunProtectionBlockFeedback: CreateShutterSunProtectionBlockFeedbackAddress(subGroup),
                SunProtectionActive: CreateShutterSunProtectionActiveAddress(subGroup),
                StopControl: CreateShutterStopAddress(subGroup),
                MovementStatusFeedback: CreateShutterMovementStatusFeedbackAddress(subGroup),
                BrightnessThreshold1: CreateBrightnessThreshold1Address(),
                BrightnessThreshold2: CreateBrightnessThreshold2Address(),
                OutdoorTemperatureThreshold: CreateOutdoorTemperatureThresholdAddress()
            );
        }

        private static string CreateShutterSunProtectionBlockFeedbackAddress(string subGroup)
        {
            var feedbackSubGroup = (int.Parse(subGroup) + SHUTTER_SUN_PROTECTION_BLOCK_FEEDBACK_OFFSET).ToString();
            return $"{SHUTTERS_MAIN_GROUP}/{SHUTTERS_SUN_PROTECTION_BLOCK_MIDDLE_GROUP}/{feedbackSubGroup}";
        }

        private static string CreateShutterSunProtectionBlockAddress(string subGroup)
        {
            return $"{SHUTTERS_MAIN_GROUP}/{SHUTTERS_SUN_PROTECTION_BLOCK_MIDDLE_GROUP}/{subGroup}";
        }

        /// <summary>
        /// Creates a complete DimmerAddresses object for a dimmer device
        /// </summary>
        /// <param name="subGroup">Sub group number (e.g., "1", "2", etc.)</param>
        /// <returns>Complete DimmerAddresses object</returns>
        public static DimmerAddresses CreateDimmerAddresses(string subGroup)
        {
            return new DimmerAddresses(
                SwitchControl: CreateDimmerSwitchControlAddress(subGroup),
                SwitchFeedback: CreateDimmerSwitchFeedbackAddress(subGroup),
                BrightnessControl: CreateDimmerBrightnessControlAddress(subGroup),
                BrightnessFeedback: CreateDimmerBrightnessFeedbackAddress(subGroup),
                LockControl: CreateDimmerLockAddress(subGroup),
                LockFeedback: CreateDimmerLockFeedbackAddress(subGroup)
            );
        }

        /// <summary>
        /// Creates a complete ClockAddresses object for a clock device
        /// </summary>
        /// <returns>Complete ClockAddresses object</returns>
        public static ClockAddresses CreateClockAddresses()
        {
            return new ClockAddresses(
                TimeControl: CreateClockTimeAddress()
            );
        }

        /// <summary>
        /// Creates a complete SunProtectionThresholdAddresses object for sun protection threshold simulation
        /// </summary>
        /// <returns>Complete SunProtectionThresholdAddresses object</returns>
        public static SunProtectionThresholdAddresses CreateSunProtectionThresholdAddresses()
        {
            return new SunProtectionThresholdAddresses(
                BrightnessThreshold1: CreateBrightnessThreshold1Address(),
                BrightnessThreshold2: CreateBrightnessThreshold2Address(),
                OutdoorTemperatureThreshold: CreateOutdoorTemperatureThresholdAddress(),
                SunProtectionActive: "" // Uzupe?nij, je?li masz adres aktywacji ochrony przeciws?onecznej
            );
        }

        #endregion
    }
}
