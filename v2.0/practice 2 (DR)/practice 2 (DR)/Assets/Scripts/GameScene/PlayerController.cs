using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class PlayerController : MonoBehaviour
{
    [SerializeField] GameManager _gameManager;

    Rigidbody2D _rb;
    Camera _mainCamera;

    float _moveVertical;
    float _moveHorizontal;
    float _moveSpeed = 5f;
    float _speedLimiter = 0.7f;

    public Vector2 _moveVelocity;
    public Vector2 _mousePosition;
    public Vector2 _offset;

    [SerializeField] GameObject _bullet;
    [SerializeField] GameObject _bulletWeapon;

    [SerializeField] GameObject _rocket;
    [SerializeField] GameObject _rocketWeapon;

    public bool _timeAvailable = true;
    public float _timer;

    public bool _isShootingBullet = false;
    public bool _isShootingRocket = false;
    float _bulletSpeed = 15f;
    float _rocketSpeed = 5f;

    [SerializeField] TMP_Text _equipped;
    void Start()
    {
        _rb = gameObject.GetComponent<Rigidbody2D>();
        _mainCamera = Camera.main;

        _bulletWeapon.SetActive(true);
        _rocketWeapon.SetActive(false);
    }

    void Update()
    {
        _moveHorizontal = Input.GetAxisRaw("Horizontal");
        _moveVertical = Input.GetAxisRaw("Vertical");

        _moveVelocity = new Vector2(_moveHorizontal, _moveVertical) * _moveSpeed;

        if (Input.GetKeyDown("1"))
        {

                _bulletWeapon.SetActive(true);
                _rocketWeapon.SetActive(false);
        }

        if (Input.GetKeyDown("2"))
        {

            _bulletWeapon.SetActive(false);
            _rocketWeapon.SetActive(true);
        }

        if (Input.GetMouseButtonDown(0))
        {
            if (_bulletWeapon.activeSelf)
            {
                _isShootingBullet = true;
            }
            if (_rocketWeapon.activeSelf)
            {
                _isShootingRocket = true;
            }
        }

        if (_bulletWeapon.activeSelf)
        {
            _equipped.text = "Bullet";
        }
        if (_rocketWeapon.activeSelf)
        {
            _equipped.text = "Rocket";
        }
    }

    private void FixedUpdate()
    {
        MovePlayer();
        RotatePlayer();

        if (_isShootingBullet)
        {
            StartCoroutine(FireBullet());
        }

        if (_isShootingRocket) // Fires rocket.
        {
            if (_timeAvailable == false)
            {
                return;
            }
            else
            {
                StartCoroutine(FireRocket());
                StartCoroutine(RocketCooldown());
            }
        }
    }

    void MovePlayer()
    {
        if (_moveHorizontal != 0 || _moveVertical != 0)
        {
            if (_moveHorizontal != 0 && _moveVertical != 0)
            {
                _moveVelocity *= _speedLimiter;
            }
            _rb.velocity = _moveVelocity;
        }
        else
        {
            _moveVelocity = new Vector2(0f, 0f);
            _rb.velocity = _moveVelocity;
        }
    }

    void RotatePlayer()
    {
        _mousePosition = Input.mousePosition;
        Vector3 screenPoint = _mainCamera.WorldToScreenPoint(transform.localPosition);
        _offset = new Vector2(_mousePosition.x - screenPoint.x, _mousePosition.y - screenPoint.y).normalized;

        float angle = Mathf.Atan2(_offset.y, _offset.x) * Mathf.Rad2Deg;

        transform.rotation = Quaternion.Euler(0f, 0f, angle - 90f);
    }

    IEnumerator FireBullet()
    {
        _isShootingBullet = false;
        GameObject Bullet = Instantiate(_bullet, _bulletWeapon.transform.position, Quaternion.identity);
        Bullet.GetComponent<Rigidbody2D>().velocity = _offset * _bulletSpeed;
        yield return new WaitForSeconds(3);
        Destroy(Bullet);
    }

    IEnumerator FireRocket() // Method to fire the rocket.
    {
        _isShootingRocket = false;
        GameObject Rocket = Instantiate(_rocket, _rocketWeapon.transform.position, transform.rotation);
        Rocket.GetComponent<Rigidbody2D>().velocity = _offset * _rocketSpeed;
        yield return new WaitForSeconds(3);
        Destroy(Rocket);
    }

    IEnumerator RocketCooldown() // Cooldowm Method for the rocket.
    {
        _timeAvailable = false;
        yield return new WaitForSeconds(_timer);
        _timeAvailable = true;
    }
}
