shader_type canvas_item;

// Attack telegraph shader for showing weapon attack areas
// Used for Requirements 10.1, 10.2: Attack telegraph effects and visual indicators

uniform float progress : hint_range(0.0, 1.0) = 0.0;
uniform float intensity : hint_range(0.0, 2.0) = 1.0;
uniform vec4 telegraph_color : source_color = vec4(1.0, 0.8, 0.2, 0.6);
uniform float pulse_speed : hint_range(0.1, 5.0) = 2.0;
uniform float fade_edge : hint_range(0.0, 0.5) = 0.1;

varying vec2 world_position;

void vertex() {
    world_position = VERTEX;
}

void fragment() {
    vec2 uv = UV;
    
    // Create pulsing effect based on time
    float pulse = sin(TIME * pulse_speed) * 0.5 + 0.5;
    
    // Calculate distance from center for radial fade
    float dist_from_center = distance(uv, vec2(0.5, 0.5));
    
    // Create edge fade effect
    float edge_fade = 1.0 - smoothstep(0.5 - fade_edge, 0.5, dist_from_center);
    
    // Combine progress animation with pulse
    float alpha = telegraph_color.a * intensity * pulse * edge_fade * progress;
    
    // Apply color with animated alpha
    COLOR = vec4(telegraph_color.rgb, alpha);
}