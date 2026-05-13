using System;

namespace KitchenDungeon.Models
{
    // ---------------------------------------------------------
    // [공통] 서버의 기본 응답 구조
    // 모든 API 응답이 status와 message를 포함한다면 상속해서 쓰기 좋아.
    // ---------------------------------------------------------
    [Serializable]
    public class BaseResponse
    {
        public string status;
        public string message;
    }

    // ---------------------------------------------------------
    // [회원가입] /api/v1/users (POST)
    // ---------------------------------------------------------

    // 회원가입 요청 데이터 (유니티 -> 서버)
    [Serializable]
    public class SignupRequest
    {
        public string username;
        public string password;
        public string nickname;
    }

    // 회원가입 응답 데이터 (서버 -> 유니티)
    [Serializable]
    public class SignupResponseData
    {
        public int user_id;
    }

    [Serializable]
    public class SignupResponse : BaseResponse
    {
        public SignupResponseData data;
    }

    // ---------------------------------------------------------
    // [로그인]
    // ---------------------------------------------------------

    // --- 로그인 요청 (유니티 -> 서버) ---
    [Serializable]
    public class LoginRequest
    {
        public string username;
        public string password;
    }

    // --- 로그인 응답 데이터 (서버 -> 유니티) ---
    [Serializable]
    public class LoginResponseData
    {
        public int user_id;
        public string nickname;
        public int total_gold;
        public int user_level;
        public int current_exp;
    }

    [Serializable]
    public class LoginResponse : BaseResponse
    {
        public LoginResponseData data;
    }

    [Serializable]
    public class UserProfile
    {
        public int user_id;
        public string nickname;
        public int level;
        public int exp;
        public int gold;
    }
}