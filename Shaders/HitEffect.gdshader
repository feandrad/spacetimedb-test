shader_type canvas_item;

// Hit effect shader for damage feedback
// Used for Requirements 10.3, 10.4: Hit confirmation and damage feedback effects

uniform float hit_progress : hint_range(0.0, 1.0) = 0.0;
uniform float flash_intensity : hint_range(0.0, 3.0) = 2.0;
uniform vec4 hit_color : source_color = vec4(1.0, 0.2, 0.2, 1.0);
uniform float shake_amount : hint_range(0.0, 10.0) = 2.0;
uniform float ring_thickness : hint_range(0.01, 0.2) = 0.05;

void fragment() {
    vec2 uv = UV;
    
    // Create expanding ring effect
    float dist_from_center = distance(uv, vec2(0.5, 0.5));
    float ring_radius = hit_progress * 0.7;
    
    // Ring effect with thickness
    float ring = 1.0 - smoothstep(ring_radius - ring_thickness, ring_radius, dist_from_center);
    ring *= smoothstep(ring_radius - ring_thickness * 2.0, ring_radius - ring_thickness, dist_from_center);
    
    // Flash effect that fades over time
    float flash = (1.0 - hit_progress) * flash_intensity;
    
    // Combine original texture with hit effects
    vec4 original_color = texture(TEXTURE, uv);
    
    // Apply hit color overlay
    vec3 hit_overlay = mix(original_color.rgb, hit_color.rgb, ring * hit_color.a);
    
    // Add flash effect
    hit_overlay += vec3(flash * (1.0 - hit_progress));
    
    COLOR = vec4(hit_overlay, original_color.a);
}