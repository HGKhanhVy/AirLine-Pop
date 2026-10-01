#ifndef AIRLINEPOP_BOARD_LIT_SHARED
#define AIRLINEPOP_BOARD_LIT_SHARED

// Board-wide lighting, set once by BoardLighting through Shader.SetGlobal*.
//
// The board lies on the XY plane with "up", towards the sky, along -Z; depth into the
// ground is +Z. A tilted perspective camera looks down on it from the near side.
float4 _AirLineLightDir;       // xyz: direction the light travels, into the board
half4 _AirLineLightColor;      // rgb: colour times intensity
half4 _AirLineSkyAmbient;      // light on faces turned to the sky
half4 _AirLineGroundAmbient;   // light on faces turned to the ground
half4 _AirLineShadowColor;     // rgb and opacity of cast shadows

#endif
