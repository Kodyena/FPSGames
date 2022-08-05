using UnityEngine;

public static class Movement
{
    public static void BasicDirectionalMove(Rigidbody rigidbody, Vector3 direction, float speed, float acceleration)
    {
        rigidbody.AddForce(direction * acceleration * Time.deltaTime, ForceMode.Force);
        Vector3 flatVelocity = new Vector3(rigidbody.velocity.x, 0, rigidbody.velocity.z);
        if(flatVelocity.magnitude > speed)
        {
            Vector3 clampedVelocity = flatVelocity.normalized * speed;
            clampedVelocity.y = rigidbody.velocity.y;
            rigidbody.velocity = Vector3.Lerp(rigidbody.velocity, clampedVelocity, clampedVelocity.magnitude / rigidbody.velocity.magnitude);
        }
    }

    public static void Slide(Rigidbody rigidbody, AnimationCurve decayCurve, Vector3 startVelocity, float slideTime, float maxSlideTime)
    {
        rigidbody.velocity = startVelocity * decayCurve.Evaluate(slideTime / maxSlideTime);
    }
}