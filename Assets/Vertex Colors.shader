// Made with Amplify Shader Editor
// Available at the Unity Asset Store - http://u3d.as/y3X 
Shader "Vertex Colors"
{
	Properties
	{
		_Alpha("Alpha", Range( 0 , 1)) = 1
		_Glow("Glow", Range( 0 , 1)) = 0
		_Cutoff( "Mask Clip Value", Float ) = 0.5
		_highlight("highlight", Color) = (1,1,1,0)
		_Shadow("Shadow", Color) = (0,0,0,0)
		_GlowHighlight("Glow Highlight", Color) = (0,0,0,0)
		_GlowShadow("Glow Shadow", Color) = (0,0,0,0)
		[HideInInspector] __dirty( "", Int ) = 1
	}

	SubShader
	{
		Tags{ "RenderType" = "TransparentCutout"  "Queue" = "Geometry+0" "IsEmissive" = "true"  }
		Cull Back
		CGPROGRAM
		#pragma target 3.0
		#pragma surface surf Standard keepalpha addshadow fullforwardshadows 
		struct Input
		{
			float4 vertexColor : COLOR;
		};

		uniform float4 _Shadow;
		uniform float4 _highlight;
		uniform float4 _GlowShadow;
		uniform float4 _GlowHighlight;
		uniform float _Glow;
		uniform float _Alpha;
		uniform float _Cutoff = 0.5;

		void surf( Input i , inout SurfaceOutputStandard o )
		{
			float4 lerpResult18 = lerp( _Shadow , _highlight , i.vertexColor);
			o.Albedo = lerpResult18.rgb;
			float4 lerpResult19 = lerp( _GlowShadow , _GlowHighlight , i.vertexColor);
			float4 lerpResult24 = lerp( float4( 0,0,0,0 ) , lerpResult19 , _Glow);
			o.Emission = lerpResult24.rgb;
			o.Alpha = 1;
			float4 temp_output_5_0 = ( ( i.vertexColor + 0.5 ) * _Alpha );
			clip( temp_output_5_0.r - _Cutoff );
		}

		ENDCG
	}
	Fallback "Diffuse"
	CustomEditor "ASEMaterialInspector"
}
/*ASEBEGIN
Version=16900
0;72.66667;894;796;1662.07;988.0931;2.292879;True;False
Node;AmplifyShaderEditor.VertexColorNode;1;-894.0079,-160.9434;Float;False;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RangedFloatNode;15;-315.4391,176.4935;Float;False;Constant;_Float1;Float 1;2;0;Create;True;0;0;False;0;0.5;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.ColorNode;20;-462.6169,-115.4337;Float;False;Property;_GlowHighlight;Glow Highlight;5;0;Create;True;0;0;False;0;0,0,0,0;0,0,0,0;True;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.ColorNode;21;-456.6705,-289.0795;Float;False;Property;_GlowShadow;Glow Shadow;6;0;Create;True;0;0;False;0;0,0,0,0;0,0,0,0;True;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleAddOpNode;16;-125.1296,156.3569;Float;False;2;2;0;COLOR;0,0,0,0;False;1;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.RangedFloatNode;4;-250.4391,304.8082;Float;False;Property;_Alpha;Alpha;0;0;Create;True;0;0;False;0;1;0.851;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp;19;-167.6124,-227.4669;Float;False;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.RangedFloatNode;23;-210.215,0.852744;Float;False;Property;_Glow;Glow;1;0;Create;True;0;0;False;0;0;0.851;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.ColorNode;17;-477.4849,-640.8696;Float;False;Property;_Shadow;Shadow;4;0;Create;True;0;0;False;0;0,0,0,0;0,0,0,0;True;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.ColorNode;9;-481.64,-465.2688;Float;False;Property;_highlight;highlight;3;0;Create;True;0;0;False;0;1,1,1,0;1,1,1,0;True;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.LerpOp;18;-184.5038,-379.6458;Float;False;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;5;94.61092,247.6455;Float;False;2;2;0;COLOR;0,0,0,0;False;1;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.DitheringNode;6;348.2843,107.0012;Float;False;0;False;3;0;FLOAT;0;False;1;SAMPLER2D;;False;2;FLOAT4;0,0,0,0;False;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp;24;89.5154,-151.8774;Float;False;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.StandardSurfaceOutputNode;3;595.0917,-87.94735;Float;False;True;2;Float;ASEMaterialInspector;0;0;Standard;Vertex Colors;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;Back;0;False;-1;0;False;-1;False;0;False;-1;0;False;-1;False;0;Custom;0.5;True;True;0;True;TransparentCutout;;Geometry;All;True;True;True;True;True;True;True;True;True;True;True;True;True;True;True;True;True;0;False;-1;False;0;False;-1;255;False;-1;255;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;False;2;15;10;25;False;0.5;True;0;0;False;-1;0;False;-1;0;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;0;0,0,0,0;VertexOffset;True;False;Cylindrical;False;Relative;0;;2;-1;-1;-1;0;False;0;0;False;-1;-1;0;False;-1;0;0;0;False;0.1;False;-1;0;False;-1;16;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;2;FLOAT3;0,0,0;False;3;FLOAT;0;False;4;FLOAT;0;False;5;FLOAT;0;False;6;FLOAT3;0,0,0;False;7;FLOAT3;0,0,0;False;8;FLOAT;0;False;9;FLOAT;0;False;10;FLOAT;0;False;13;FLOAT3;0,0,0;False;11;FLOAT3;0,0,0;False;12;FLOAT3;0,0,0;False;14;FLOAT4;0,0,0,0;False;15;FLOAT3;0,0,0;False;0
WireConnection;16;0;1;0
WireConnection;16;1;15;0
WireConnection;19;0;21;0
WireConnection;19;1;20;0
WireConnection;19;2;1;0
WireConnection;18;0;17;0
WireConnection;18;1;9;0
WireConnection;18;2;1;0
WireConnection;5;0;16;0
WireConnection;5;1;4;0
WireConnection;6;0;5;0
WireConnection;24;1;19;0
WireConnection;24;2;23;0
WireConnection;3;0;18;0
WireConnection;3;2;24;0
WireConnection;3;10;5;0
ASEEND*/
//CHKSM=1F8F86C2369083DE7C57B1B5773D405DE9600F65