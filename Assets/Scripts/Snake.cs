using Globals;
using SteeringCalcs;
using UnityEngine;

public class Snake : MonoBehaviour
{
    // Obstacle avoidance parameters (see the assignment spec for an explanation).
    public AvoidanceParams AvoidParams;

    // Steering parameters.
    public float MaxSpeed;
    public float MaxAccel;
    public float AccelTime;

    // Use this as the arrival radius for all states where the steering behaviour == arrive.
    public float ArriveRadius;

    // Parameters controlling transitions in/out of the Aggro state.
    public float AggroRange;
    public float DeAggroRange;

    // Reference to the frog (the target for the Aggro state).
    public GameObject Frog;

    // The patrol point (the target for the PatrolAway state).
    public Transform PatrolPoint;

    // The snake's initial position (the target for the PatrolHome and Harmless states).
    private Vector2 _home;

    // Debug rendering config
    private float _debugHomeOffset = 0.3f;

    //timer for snake snoozing time
    private float _timer = 0.0f;

    // References for gameobject controls
    private Rigidbody2D _rb;
    private SpriteRenderer _sr;
    private Animator _animator;

    // Current Snake FSM State
    public SnakeState State;

    // Snake FSM states (to be completed by you)
    public enum SnakeState : int
    {
        PatrolAway = 0,
        PatrolHome = 1,
        Attack = 2,
        Harmless = 3,
        Snooze = 4,
        Fleeing = 5,
    }

    // Snake FSM events (to be completed by you)
    public enum SnakeEvent : int
    {
        FrogInRange = 0,
        FrogOutOfRange = 1,
        BitFrog = 2,
        ReachedTarget = 3,
        TimerOff = 4,
        HitByBubble = 5,
        NotScared = 6,
    }

    // Direction IDs used by the snake animator (please don't edit these).
    private enum Direction : int
    {
        Up = 0,
        Left = 1,
        Down = 2,
        Right = 3
    }

    void Start()
    {
        _rb = GetComponent<Rigidbody2D>();
        _sr = GetComponent<SpriteRenderer>();
        _animator = GetComponent<Animator>();

        _home = transform.position;
    }

    // Our common FSM approach has been setup for you.
    // This is an event-first FSM, where events can be triggered by FixedUpdateEvents().
    // Then FSM_State() processes the current FSM state.
    // UpdateAppearance() is called at the end to update the snake's appearance.
    void FixedUpdate()
    {
        // Events triggered by each fixed update tick
        FixedUpdateEvents();

        // Update the Snake behaviour based on the current FSM state
        FSM_State();

        // Configure final appearance of the snake
        UpdateAppearance();
    }

    // Trigger Events for each fixed update tick, using a trigger first FSM implementation
    void FixedUpdateEvents()
    {
        float distToFrog = (transform.position - Frog.transform.position).magnitude;

        //If Snake is in Patrol State, check if frog is in aggro range
        if (State == SnakeState.PatrolAway || State == SnakeState.PatrolHome)
        {
            if (distToFrog < AggroRange)
            {
                HandleEvent(SnakeEvent.FrogInRange);
            }
        }

        //If Snake is in Attack State, check if frog is out of aggro range
        if (State == SnakeState.Attack)
        {
            if (distToFrog > DeAggroRange)
            {
                HandleEvent(SnakeEvent.FrogOutOfRange);
            }
        }

        //Check if snake reached patrol target when it leaves its home
        if (State == SnakeState.PatrolAway)
        {
            if ((transform.position - PatrolPoint.position).magnitude < Constants.TARGET_REACHED_TOLERANCE)
            {
                HandleEvent(SnakeEvent.ReachedTarget);
            }
        }

        //Check if snake reaches home when its done reaching patrol point or harmless
        if (State == SnakeState.PatrolHome || State == SnakeState.Harmless)
        {
            if (((Vector2)transform.position - _home).magnitude < Constants.TARGET_REACHED_TOLERANCE)
            {
                HandleEvent(SnakeEvent.ReachedTarget);
            }
        }

        //Increment Snakes timer when snoozing
        if (State == SnakeState.Snooze)
        {
            _timer += Time.fixedDeltaTime;
            if (_timer >= Constants.SLEEP_TIME)
            {
                HandleEvent(SnakeEvent.TimerOff);
            }
        }

        //Checks if its far enough from frog
        if (State == SnakeState.Fleeing)
        {
            if (distToFrog > DeAggroRange)
            {
                HandleEvent(SnakeEvent.NotScared);
            }
        }
    }


    // Process the current FSM state, using an event first FSM implementation
    // This currently has a zero steering force.
    // You need to implement the steering logic depending on the FSM state.
    void FSM_State()
    {
        Vector2 desiredVel = Vector2.zero;

        if (State == SnakeState.PatrolAway)
        {
            desiredVel = Steering.Arrive(transform.position, PatrolPoint.position, ArriveRadius, MaxSpeed, AvoidParams);
        }
        else if (State == SnakeState.PatrolHome)
        {
            desiredVel = Steering.Arrive(transform.position, _home, ArriveRadius, MaxSpeed, AvoidParams);
        }
        else if (State == SnakeState.Attack)
        {
            desiredVel = Steering.Seek(transform.position, Frog.transform.position, MaxSpeed, AvoidParams);
        }
        else if (State == SnakeState.Harmless)
        {
            desiredVel = Steering.Arrive(transform.position, _home, ArriveRadius, MaxSpeed, AvoidParams);
        }
        else if (State == SnakeState.Snooze)
        {
            desiredVel = Vector2.zero;
        }
        else if (State == SnakeState.Fleeing)
        {
            desiredVel = Steering.Flee(transform.position, Frog.transform.position, MaxSpeed, AvoidParams);
        }

        // Convert the desired velocity to a force, then apply it.
        Vector2 steering = Steering.DesiredVelToForce(desiredVel, _rb, AccelTime, MaxAccel);
        _rb.AddForce(steering);
    }

    private void SetState(SnakeState newState)
    {
        if (newState != State)
        {
            // Can uncomment this for debugging purposes.
            //Debug.Log(name + " switching state to " + newState.ToString());

            State = newState;
            _timer = 0.0f; //reset timer so snake wont wake up instantly
        }
    }

    private void HandleEvent(SnakeEvent e)
    {
        if (State == SnakeState.PatrolAway)
        {
            if (e == SnakeEvent.FrogInRange)
            {
                SetState(SnakeState.Attack);
            }
            else if (e == SnakeEvent.ReachedTarget)
            {
                SetState(SnakeState.PatrolHome);
            }
        }
        else if (State == SnakeState.PatrolHome)
        {
            if (e == SnakeEvent.FrogInRange)
            {
                SetState(SnakeState.Attack);
            }
            else if (e == SnakeEvent.ReachedTarget)
            {
                SetState(SnakeState.PatrolAway);
            }
        }
        else if (State == SnakeState.Attack)
        {
            if (e == SnakeEvent.FrogOutOfRange)
            {
                SetState(SnakeState.PatrolHome);
            }
            else if (e == SnakeEvent.BitFrog)
            {
                SetState(SnakeState.Snooze);
            }
        }
        else if (State == SnakeState.Harmless)
        {
            if (e == SnakeEvent.ReachedTarget)
            {
                SetState(SnakeState.PatrolHome);
            }
        }
        else if (State == SnakeState.Snooze)
        {
            if (e == SnakeEvent.TimerOff)
            {
                SetState(SnakeState.Harmless);
            }
        }

        //If Snake is hit by a bubble and not fleeing already it flees
        if (e == SnakeEvent.HitByBubble && State != SnakeState.Fleeing)
        {
            SetState(SnakeState.Fleeing);
        }

        //When the snake is not scared transitions to patrol home state
        if (e == SnakeEvent.NotScared && State == SnakeState.Fleeing)
        {
            SetState(SnakeState.PatrolHome);
        }

    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.tag.Equals("Frog"))
        {
            HandleEvent(SnakeEvent.BitFrog);
        }
    }
    //Bubble destroyed when hits
    private void OnTriggerEnter2D(Collider2D collider)
    {
        if (collider.gameObject.tag.Equals("Bubble"))
        {
            HandleEvent(SnakeEvent.HitByBubble);
            Destroy(collider.gameObject);
        }
    }

    private void UpdateAppearance()
    {
        // Update the snake's colour to provide a visual indication of its state.
        // This is for you to implement
        if (State == SnakeState.PatrolAway)
        {
            _sr.enabled = true;
            _sr.color = new Color(0.5f, 0.5f, 0.5f);
        }
        else if (State == SnakeState.PatrolHome)
        {
            _sr.enabled = true;
            _sr.color = new Color(1.0f, 1.0f, 1.0f);
        }
        else if (State == SnakeState.Attack)
        {
            _sr.enabled = true;
            _sr.color = new Color(1.0f, 0.1f, 0.1f);
        }
        else if (State == SnakeState.Harmless)
        {
            _sr.enabled = true;
            _sr.color = new Color(0.2f, 0.9f, 0.2f);
        }
        else if (State == SnakeState.Snooze)
        {
            _sr.enabled = true;
            _sr.color = new Color(0.2f, 0.2f, 0.9f);
        }
        else if (State == SnakeState.Fleeing)
        {
            _sr.enabled = true;
            _sr.color = new Color(0.9f, 0.7f, 0.2f);
        }

        // Update the Snake visual based on the direction it's moving
        // (please don't modify this block)
        if (_rb.linearVelocity.magnitude > Constants.MIN_SPEED_TO_ANIMATE)
        {
            // Determine the bearing of the snake in degrees (between -180 and 180)
            float angle = Mathf.Atan2(_rb.linearVelocity.y, _rb.linearVelocity.x) * Mathf.Rad2Deg;

            if (angle > -135.0f && angle <= -45.0f) // Down
            {
                transform.up = new Vector2(0.0f, -1.0f);
                _animator.SetInteger("Direction", (int)Direction.Down);
            }
            else if (angle > -45.0f && angle <= 45.0f) // Right
            {
                transform.up = new Vector2(1.0f, 0.0f);
                _animator.SetInteger("Direction", (int)Direction.Right);
            }
            else if (angle > 45.0f && angle <= 135.0f) // Up
            {
                transform.up = new Vector2(0.0f, 1.0f);
                _animator.SetInteger("Direction", (int)Direction.Up);
            }
            else // Left
            {
                transform.up = new Vector2(-1.0f, 0.0f);
                _animator.SetInteger("Direction", (int)Direction.Left);
            }
        }

        // Display the Snake home position as a cross
        Debug.DrawLine(_home + new Vector2(-_debugHomeOffset, -_debugHomeOffset), _home + new Vector2(_debugHomeOffset, _debugHomeOffset), Color.magenta);
        Debug.DrawLine(_home + new Vector2(-_debugHomeOffset, _debugHomeOffset), _home + new Vector2(_debugHomeOffset, -_debugHomeOffset), Color.magenta);
    }
}
