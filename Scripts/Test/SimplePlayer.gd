extends CharacterBody2D

@export var speed = 200.0

func _ready():
	print("SimplePlayer: Ready - GDScript version")

func _physics_process(delta):
	# Get input direction
	var direction = Vector2.ZERO
	
	if Input.is_action_pressed("ui_up") or Input.is_key_pressed(KEY_W):
		direction.y -= 1.0
		print("Moving UP")
	if Input.is_action_pressed("ui_down") or Input.is_key_pressed(KEY_S):
		direction.y += 1.0
		print("Moving DOWN")
	if Input.is_action_pressed("ui_left") or Input.is_key_pressed(KEY_A):
		direction.x -= 1.0
		print("Moving LEFT")
	if Input.is_action_pressed("ui_right") or Input.is_key_pressed(KEY_D):
		direction.x += 1.0
		print("Moving RIGHT")
	
	# Normalize diagonal movement
	if direction.length() > 1.0:
		direction = direction.normalized()
	
	# Apply movement
	velocity = direction * speed
	move_and_slide()
	
	# Update color based on movement
	var player_sprite = get_node_or_null("PlayerSprite")
	if player_sprite:
		if velocity.length() > 0.1:
			player_sprite.color = Color.LIGHT_BLUE
		else:
			player_sprite.color = Color.BLUE
	
	# Debug output
	if direction != Vector2.ZERO:
		print("Moving: direction=", direction, " velocity=", velocity, " position=", position)