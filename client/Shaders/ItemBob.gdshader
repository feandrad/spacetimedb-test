shader_type canvas_item;

// Item bob animation shader for collectible items
// Used for Requirements 10.7: Simple visual effects and animations through shaders

uniform float bob_speed : hint_range(0.5, 5.0) = 2.0;
uniform float bob_height : hint_range(1.0, 20.0) = 5.0;
uniform float glow_intensity : hint_range(0.0, 2.0) = 0.5;
uniform vec4 glow_color : source_color = vec4(1.0, 1.0, 0.8, 0.3);
uniform float rotation_speed : hint_range(0.0, 2.0) = 0.5;

varying vec2 world_position;

void vertex() {
    world_position = VERTEX;
    
    // Apply vertical bobbing animation
    float bob_offset = sin(TIME * bob_speed) * bob_height;
    VERTEX.y += bob_offset;
    
    // Apply gentle rotation
    if (rotation_speed > 0.0) {
        float angle = TIME * rotation_speed;
        mat2 rotation_matrix = mat2(vec2(cos(angle), -sin(angle)), vec2(sin(angle), cos(angle)));
        VERTEX = rotation_matrix * VERTEX;
    }
}

void fragment() {
    vec2 uv = UV;
    
    // Get original texture
    vec4 original_color = texture(TEXTURE, uv);
    
    // Create pulsing glow effect
    float glow_pulse = sin(TIME * bob_speed * 1.5) * 0.5 + 0.5;
    
    // Calculate distance from center for glow
    float dist_from_center = distance(uv, vec2(0.5, 0.5));
    float glow_mask = 1.0 - smoothstep(0.3, 0.7, dist_from_center);
    
    // Apply glow effect
    vec3 glow_effect = glow_color.rgb * glow_intensity * glow_pulse * glow_mask;
    
    // Combine original color with glow
    vec3 final_color = original_color.rgb + glow_effect;
    
    COLOR = vec4(final_color, original_color.a);
}