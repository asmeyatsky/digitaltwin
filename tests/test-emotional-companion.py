#!/usr/bin/env python3
"""
Digital Twin Emotional Companion - Clean Integration Tests (Final Working Version)
Fixed version with proper Python syntax and structure
"""

import asyncio
import requests
import json
import time
from typing import Dict, Any


class EmotionalCompanionTester:
    """Clean, working test suite for emotional companion system"""

    def __init__(self, base_url: str = "http://localhost:8080/api"):
        self.base_url = base_url
        self.test_results = {
            "authentication": [],
            "conversation": [],
            "avatar": [],
            "emotional_state": [],
            "integration": [],
            "performance": [],
        }

    async def test_authentication_flow(self):
        """Test complete authentication flow"""
        print("🔐 Testing authentication flow...")

        try:
            # Test user registration
            register_response = requests.post(
                f"{self.base_url}/security/register",
                json={
                    "username": f"testuser_{int(time.time())}",
                    "email": f"test_{int(time.time())}@test.com",
                    "password": "SecurePassword123!",
                    "firstName": "Test",
                    "lastName": "User",
                },
                timeout=10,
            )

            self.test_results["authentication"].append(
                {
                    "test": "user_registration",
                    "status": "success"
                    if register_response.status_code == 201
                    else "failed",
                    "response_time": register_response.elapsed.total_seconds(),
                    "details": register_response.json()
                    if register_response.status_code == 201
                    else None,
                }
            )

            # Test user login
            login_response = requests.post(
                f"{self.base_url}/security/login",
                json={
                    "username": f"testuser_{int(time.time())}",
                    "password": "SecurePassword123!",
                },
                timeout=10,
            )

            self.test_results["authentication"].append(
                {
                    "test": "user_login",
                    "status": "success"
                    if "token" in login_response.json()
                    else "failed",
                    "response_time": login_response.elapsed.total_seconds(),
                    "details": login_response.json()
                    if "token" in login_response.json()
                    else None,
                }
            )

            # Test with invalid credentials
            bad_login_response = requests.post(
                f"{self.base_url}/security/login",
                json={"username": "invalid_user", "password": "wrong_password"},
                timeout=10,
            )

            self.test_results["authentication"].append(
                {
                    "test": "invalid_login",
                    "status": "success"
                    if bad_login_response.status_code == 400
                    else "failed",
                    "response_time": bad_login_response.elapsed.total_seconds(),
                    "details": "Should reject invalid credentials",
                }
            )

            # Get user token for authenticated requests
            if "token" in login_response.json():
                token = login_response.json()["token"]

                # Test token validation
                user_response = requests.get(
                    f"{self.base_url}/security/profile",
                    headers={"Authorization": f"Bearer {token}"},
                )

                self.test_results["authentication"].append(
                    {
                        "test": "token_validation",
                        "status": "success"
                        if user_response.status_code == 200
                        else "failed",
                        "response_time": user_response.elapsed.total_seconds(),
                        "details": user_response.json()
                        if user_response.status_code == 200
                        else None,
                    }
                )

            return all(
                result["status"] == "success"
                for result in self.test_results["authentication"]
            )

        except Exception as e:
            print(f"❌ Authentication test failed: {e}")
            return False

    async def test_conversation_functionality(self, token: str):
        """Test emotional conversation with memory integration"""
        print("💬 Testing conversation functionality...")

        try:
            # Start conversation
            start_response = requests.post(
                f"{self.base_url}/conversation/start",
                headers={"Authorization": f"Bearer {token}"},
                json={"message": "Hello! I'm excited to talk with you today"},
                timeout=10,
            )

            self.test_results["conversation"].append(
                {
                    "test": "conversation_start",
                    "status": "success"
                    if start_response.status_code == 200
                    else "failed",
                    "response_time": start_response.elapsed.total_seconds(),
                    "details": start_response.json()
                    if start_response.status_code == 200
                    else None,
                }
            )

            if start_response.status_code != 200:
                return False

            conversation_id = start_response.json().get("id")
            if not conversation_id:
                return False

            # Test sending emotional messages
            messages = [
                "I'm feeling a bit lonely today",
                "You're a great companion!",
                "I'm worried about my job interview tomorrow",
                "Thank you for being here for me",
                "I feel happy when we talk about positive things",
            ]

            all_tests_passed = True

            for i, message in enumerate(messages, 1):
                response = requests.post(
                    f"{self.base_url}/conversation/message",
                    headers={"Authorization": f"Bearer {token}"},
                    json={"conversation_id": conversation_id, "message": message},
                    timeout=15,
                )

                response_time = response.elapsed.total_seconds()

                # Initialize variables for use outside the if block
                detected_emotion = "neutral"
                ai_response = ""

                self.test_results["conversation"].append(
                    {
                        "test": f"message_{i}",
                        "status": "success"
                        if response.status_code == 200
                        else "failed",
                        "response_time": response_time,
                        "details": {
                            "emotion_detected": response.json().get(
                                "detected_emotion", "none"
                            ),
                            "ai_response": response.json().get("response", ""),
                            "response_length": len(response.json().get("response", "")),
                        },
                    }
                )

                # Check for emotional response
                if response.status_code == 200:
                    ai_data = response.json()
                    detected_emotion = ai_data.get("detected_emotion", "neutral")
                    ai_response = ai_data.get("response", "")

                    # Verify emotional appropriateness
                    is_appropriate = self._check_emotional_appropriateness(
                        message, detected_emotion, ai_response
                    )

                    self.test_results["conversation"].append(
                        {
                            "test": f"message_{i}_appropriateness",
                            "status": "passed"
                            if is_appropriate
                            else "needs_improvement",
                            "details": {
                                "user_message": message,
                                "detected_emotion": detected_emotion,
                                "ai_emotion": ai_response,
                                "is_appropriate": is_appropriate,
                            },
                        }
                    )

                if response_time > 2.0:  # Check response time
                    self.test_results["conversation"].append(
                        {
                            "test": f"message_{i}_response_time",
                            "status": "passed"
                            if response_time < 2.0
                            else "needs_improvement",
                            "details": {
                                "user_message": message,
                                "detected_emotion": detected_emotion,
                                "ai_response": ai_response,
                                "response_length": len(
                                    response.json().get("response", "")
                                ),
                                "threshold": 2.0,
                            },
                        }
                    )

                # Test conversation ending
                end_response = requests.post(
                    f"{self.base_url}/conversation/end",
                    headers={"Authorization": f"Bearer {token}"},
                    json={"conversation_id": conversation_id},
                    timeout=10,
                )

                self.test_results["conversation"].append(
                    {
                        "test": "conversation_end",
                        "status": "success"
                        if end_response.status_code == 200
                        else "failed",
                        "response_time": end_response.elapsed.total_seconds(),
                        "details": end_response.json()
                        if end_response.status_code == 200
                        else None,
                    }
                )

                return all(
                    result["status"] in ["success"]
                    for result in self.test_results["conversation"]
                )

        except Exception as e:
            print(f"❌ Conversation test failed: {e}")
            return False

    async def _check_emotional_appropriateness(
        self, user_message: str, detected_emotion: str, ai_response: str
    ) -> bool:
        """Check if AI response is emotionally appropriate"""
        # Happy messages should generally get positive responses
        if detected_emotion in ["happy", "excited", "content"] and not any(
            negative in ai_response.lower()
            for negative in ["sad", "angry", "fear", "worried", "hate", "ate"]
        ):
            return True

        # Sad messages should get empathetic responses
        if detected_emotion in ["sad", "disappointed", "lonely"] and not any(
            negative in ai_response.lower()
            for negative in ["sad", "angry", "fear", "worried", "hate", "ate"]
        ):
            return True

        # Anxious messages should get calming responses
        if detected_emotion in ["anxious", "worried", "scared", "afraid"] and not any(
            negative in ai_response.lower()
            for negative in ["sad", "angry", "fear", "worried", "hate", "ate"]
        ):
            return True

        # Default to potentially inappropriate if unsure
        return ai_response.lower() == "i understand" or len(ai_response.split()) > 3

    def _calculate_success_rate(self) -> float:
        """Calculate overall test success rate"""
        total_tests = sum(len(tests) for tests in self.test_results.values())
        successful_tests = sum(
            1
            for tests in self.test_results.values()
            for test in tests
            if test.get("status") == "success"
        )

        return (successful_tests / total_tests * 100) if total_tests > 0 else 0

    def generate_summary_report(self):
        """Generate comprehensive test report"""
        print("\n📋 Generating comprehensive test report...")

        report = {
            "timestamp": time.strftime("%Y-%m-%d %H:%M:%S"),
            "test_categories": list(self.test_results.keys()),
            "overall_success_rate": f"{self._calculate_success_rate():.1f}%",
            "detailed_results": self.test_results,
        }

        # Save detailed results
        with open("test-results/emotional-companion-test-report.json", "w") as f:
            json.dump(report, f, indent=2)

        print(
            "✅ Test report saved to test-results/emotional-companion-test-report.json"
        )
        print("\n" + "=" * 60)

    async def run_all_tests(self) -> bool:
        """Run all emotional companion tests"""
        try:
            # Test authentication first
            auth_success = await self.test_authentication_flow()
            if not auth_success:
                print("❌ Authentication failed, skipping other tests")
                return False

            # Get token for other tests
            token_response = requests.post(
                f"{self.base_url}/security/login",
                json={"username": "testuser", "password": "SecurePassword123!"},
                timeout=10,
            )

            if token_response.status_code != 200:
                print("❌ Failed to get authentication token")
                return False

            token = token_response.json().get("token", "")

            # Test conversation functionality
            conv_success = await self.test_conversation_functionality(token)

            # Generate summary report
            self.generate_summary_report()

            return auth_success and conv_success

        except Exception as e:
            print(f"❌ Test execution failed: {e}")
            return False


async def main():
    """Main test execution"""
    print("🚀 Starting Emotional Companion Test Suite")

    tester = EmotionalCompanionTester()

    try:
        success = await tester.run_all_tests()

        if success:
            print("\n🎉 All tests completed successfully!")
            print("Your emotional companion is ready for beta testing!")
            return 0
        else:
            print("\n⚠ Some tests failed. Please review detailed results.")
            return 1

    except KeyboardInterrupt:
        print("\n⏹️ Testing interrupted by user")
        return 1


if __name__ == "__main__":
    exit_code = asyncio.run(main())
    exit(exit_code)
