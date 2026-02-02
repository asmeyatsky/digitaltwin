#!/usr/bin/env python3
"""
Digital Twin - Comprehensive Testing Framework
Quality assurance testing for emotional companion system
"""

import os
import sys
import subprocess
import asyncio
import pytest
import time
import requests
from pathlib import Path

# Add project root to path
project_root = Path(__file__).parent.parent
sys.path.insert(0, str(project_root))


class DigitalTwinTestFramework:
    """Comprehensive testing framework for Digital Twin"""

    def __init__(self):
        self.base_url = os.getenv("API_BASE_URL", "http://localhost:8080/api")
        self.test_results = {
            "unit": {},
            "integration": {},
            "performance": {},
            "security": {},
            "ml_model": {},
        }

    async def run_unit_tests(self):
        """Run unit tests for all components"""
        print("🧪 Running unit tests...")

        # .NET API Tests
        try:
            result = subprocess.run(
                [
                    "dotnet",
                    "test",
                    "--logger",
                    "junit",
                    "--results-directory",
                    "test-results/unit",
                ],
                capture_output=True,
                text=True,
                cwd=project_root,
            )
            self.test_results["unit"]["dotnet"] = result.returncode == 0
            print(
                f".NET Tests: {'✅ PASSED' if result.returncode == 0 else '❌ FAILED'}"
            )
        except Exception as e:
            print(f".NET Tests Error: {e}")
            self.test_results["unit"]["dotnet"] = False

        # Python Service Tests
        try:
            result = subprocess.run(
                [
                    "python",
                    "-m",
                    "pytest",
                    "tests/unit/",
                    "--junitxml=test-results/unit/python.xml",
                    "--cov=services",
                    "--cov-report=xml",
                    "--cov-report=html",
                ],
                capture_output=True,
                text=True,
                cwd=project_root,
            )
            self.test_results["unit"]["python"] = result.returncode == 0
            print(
                f"Python Tests: {'✅ PASSED' if result.returncode == 0 else '❌ FAILED'}"
            )
        except Exception as e:
            print(f"Python Tests Error: {e}")
            self.test_results["unit"]["python"] = False

    async def run_integration_tests(self):
        """Run integration tests between services"""
        print("🔗 Running integration tests...")

        # Test API Gateway to ML Services
        ml_services = ["deepface", "avatar-generation", "voice-service"]

        for service in ml_services:
            try:
                health_url = (
                    f"http://localhost:800{ml_services.index(service) + 1}/health"
                )
                response = requests.get(health_url, timeout=5)
                self.test_results["integration"][service] = response.status_code == 200
                print(
                    f"{service} Health: {'✅ PASSED' if response.status_code == 200 else '❌ FAILED'}"
                )
            except Exception as e:
                print(f"{service} Health Error: {e}")
                self.test_results["integration"][service] = False

    async def run_performance_tests(self):
        """Run performance and load tests"""
        print("⚡ Running performance tests...")

        # API Load Testing
        try:
            result = subprocess.run(
                [
                    "python",
                    "-m",
                    "locust",
                    "-f",
                    "tests/performance/api-load-test.py",
                    "--headless",
                    "--users",
                    "100",
                    "--spawn-rate",
                    "10",
                    "--run-time",
                    "60",
                    "--host",
                    self.base_url.replace("/api", ""),
                ],
                capture_output=True,
                text=True,
                cwd=project_root,
            )

            # Parse results (simplified)
            performance_ok = "RPS" in result.stdout
            self.test_results["performance"]["api_load"] = performance_ok
            print(f"API Load Test: {'✅ PASSED' if performance_ok else '❌ FAILED'}")
        except Exception as e:
            print(f"Performance Test Error: {e}")
            self.test_results["performance"]["api_load"] = False

        # ML Service Performance
        for service in ["deepface", "avatar-generation", "voice-service"]:
            try:
                start_time = time.time()
                response = requests.post(
                    f"http://localhost:800{ml_services.index(service) + 1}/analyze",
                    json={"test_data": "performance_test"},
                    timeout=10,
                )
                end_time = time.time()
                response_time = (end_time - start_time) * 1000  # Convert to ms

                performance_ok = (
                    response.status_code == 200 and response_time < 1000
                )  # 1 second
                self.test_results["performance"][f"{service}_response"] = performance_ok
                print(
                    f"{service} Response Time: {'✅ PASSED' if performance_ok else '❌ FAILED'} ({response_time:.0f}ms)"
                )
            except Exception as e:
                print(f"{service} Performance Error: {e}")
                self.test_results["performance"][f"{service}_response"] = False

    async def run_security_tests(self):
        """Run security vulnerability scans"""
        print("🔒 Running security tests...")

        # SQL Injection Tests
        sql_payloads = [
            "'; DROP TABLE users; --",
            "' OR '1'='1",
            "1 UNION SELECT * FROM users --",
        ]

        security_ok = True
        for payload in sql_payloads:
            try:
                response = requests.post(
                    f"{self.base_url}/security/login",
                    json={"username": payload, "password": "test"},
                    timeout=5,
                )
                if response.status_code != 400:  # Should reject SQL injection
                    security_ok = False
                    print(f"SQL Injection Test: ❌ FAILED (Accepted malicious payload)")
            except Exception as e:
                print(f"SQL Injection Test Error: {e}")
                security_ok = False

        self.test_results["security"]["sql_injection"] = security_ok
        print(
            f"SQL Injection Protection: {'✅ PASSED' if security_ok else '❌ FAILED'}"
        )

        # XSS Protection Tests
        xss_payloads = [
            "<script>alert('xss')</script>",
            "javascript:alert('xss')",
            "<img src=x onerror=alert('xss')>",
        ]

        for payload in xss_payloads:
            try:
                response = requests.post(
                    f"{self.base_url}/security/register",
                    json={"username": payload, "email": "test@test.com"},
                    timeout=5,
                )
                if response.status_code != 400:  # Should reject XSS
                    security_ok = False
                    print(
                        f"XSS Protection Test: ❌ FAILED (Accepted malicious payload)"
                    )
            except Exception as e:
                print(f"XSS Protection Test Error: {e}")
                security_ok = False

        self.test_results["security"]["xss_protection"] = security_ok
        print(f"XSS Protection: {'✅ PASSED' if security_ok else '❌ FAILED'}")

        # Authentication Tests
        try:
            # Test weak password handling
            response = requests.post(
                f"{self.base_url}/security/login",
                json={"username": "testuser", "password": "123456"},
                timeout=5,
            )
            auth_security_ok = (
                response.status_code == 400
            )  # Should reject weak password
            print(
                f"Weak Password Protection: {'✅ PASSED' if auth_security_ok else '❌ FAILED'}"
            )
        except Exception as e:
            print(f"Authentication Test Error: {e}")
            auth_security_ok = False

        self.test_results["security"]["authentication"] = auth_security_ok

    async def run_ml_model_tests(self):
        """Test ML model accuracy and performance"""
        print("🤖 Running ML model tests...")

        # Test emotion detection accuracy
        test_emotion_data = [
            {
                "audio": "test_audio_happy.wav",
                "expected_emotion": "happy",
                "confidence_threshold": 0.8,
            },
            {
                "audio": "test_audio_sad.wav",
                "expected_emotion": "sad",
                "confidence_threshold": 0.7,
            },
            {
                "audio": "test_audio_angry.wav",
                "expected_emotion": "angry",
                "confidence_threshold": 0.75,
            },
        ]

        ml_accuracy_ok = True
        for test_case in test_emotion_data:
            try:
                response = requests.post(
                    "http://localhost:8001/analyze_emotion",
                    files={"audio": open(f"tests/data/{test_case['audio']}", "rb")},
                    timeout=10,
                )

                if response.status_code == 200:
                    result = response.json()
                    predicted_emotion = result.get("emotion", "")
                    confidence = result.get("confidence", 0)

                    if (
                        predicted_emotion != test_case["expected_emotion"]
                        or confidence < test_case["confidence_threshold"]
                    ):
                        ml_accuracy_ok = False
                        print(
                            f"Emotion Detection Test: ❌ FAILED (Expected: {test_case['expected_emotion']}, Got: {predicted_emotion}, Confidence: {confidence:.2f})"
                        )
                    else:
                        print(
                            f"Emotion Detection Test: ✅ PASSED (Emotion: {predicted_emotion}, Confidence: {confidence:.2f})"
                        )
                else:
                    ml_accuracy_ok = False
                    print(
                        f"Emotion Detection Test: ❌ FAILED (HTTP {response.status_code})"
                    )
            except Exception as e:
                print(f"Emotion Detection Test Error: {e}")
                ml_accuracy_ok = False

        self.test_results["ml_model"]["emotion_detection"] = ml_accuracy_ok

    async def run_end_to_end_tests(self):
        """Run complete user journey tests"""
        print("🎭 Running end-to-end tests...")

        e2e_tests = [
            {
                "name": "User Registration and Login",
                "test_func": self.test_user_registration_flow,
            },
            {
                "name": "Avatar Customization and Voice Cloning",
                "test_func": self.test_avatar_customization_flow,
            },
            {
                "name": "Emotional Conversation",
                "test_func": self.test_emotional_conversation_flow,
            },
        ]

        for test in e2e_tests:
            try:
                result = await test["test_func"]()
                self.test_results["integration"][
                    f"e2e_{test['name'].replace(' ', '_').lower()}"
                ] = result
                print(
                    f"E2E Test - {test['name']}: {'✅ PASSED' if result else '❌ FAILED'}"
                )
            except Exception as e:
                print(f"E2E Test Error - {test['name']}: {e}")
                self.test_results["integration"][
                    f"e2e_{test['name'].replace(' ', '_').lower()}"
                ] = False

    async def test_user_registration_flow(self):
        """Test complete user registration and login flow"""
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

        if register_response.status_code != 201:
            return False

        user_data = register_response.json()

        # Test user login
        login_response = requests.post(
            f"{self.base_url}/security/login",
            json={"username": user_data["username"], "password": "SecurePassword123!"},
            timeout=10,
        )

        return login_response.status_code == 200

    async def test_avatar_customization_flow(self):
        """Test avatar customization and generation"""
        # This would test the complete avatar creation pipeline
        try:
            # Upload photo for avatar generation
            response = requests.post(
                "http://localhost:8002/generate_avatar",
                files={"photo": open("tests/data/test_photo.jpg", "rb")},
                timeout=30,
            )

            if response.status_code != 200:
                return False

            avatar_data = response.json()

            # Test voice cloning
            voice_response = requests.post(
                "http://localhost:8003/clone_voice",
                json={
                    "voice_sample": "test_voice.wav",
                    "avatar_id": avatar_data.get("id"),
                },
                timeout=30,
            )

            return voice_response.status_code == 200
        except Exception:
            return False

    async def test_emotional_conversation_flow(self):
        """Test emotional conversation with AI companion"""
        try:
            # Start conversation
            response = requests.post(
                f"{self.base_url}/conversation/start",
                json={"message": "I'm feeling a bit lonely today"},
                timeout=10,
            )

            if response.status_code != 200:
                return False

            conversation_data = response.json()
            conversation_id = conversation_data.get("id")

            # Send emotional message and check response
            emotional_response = requests.post(
                f"{self.base_url}/conversation/message",
                json={
                    "conversation_id": conversation_id,
                    "message": "I really appreciate you being here for me",
                },
                timeout=15,
            )

            return emotional_response.status_code == 200
        except Exception:
            return False

    def generate_test_report(self):
        """Generate comprehensive test report"""
        print("📊 Generating test report...")

        # Calculate overall health scores
        unit_pass_rate = sum(self.test_results["unit"].values()) / len(
            self.test_results["unit"]
        )
        integration_pass_rate = sum(self.test_results["integration"].values()) / len(
            self.test_results["integration"]
        )
        security_pass_rate = sum(self.test_results["security"].values()) / len(
            self.test_results["security"]
        )

        report = f"""
# Digital Twin Test Report
Generated: {time.strftime("%Y-%m-%d %H:%M:%S")}

## 🧪 Unit Tests
- .NET Tests: {"✅ PASSED" if self.test_results["unit"].get("dotnet", False) else "❌ FAILED"}
- Python Tests: {"✅ PASSED" if self.test_results["unit"].get("python", False) else "❌ FAILED"}
- Overall Unit Pass Rate: {unit_pass_rate:.1%}

## 🔗 Integration Tests
- DeepFace Service: {"✅ PASSED" if self.test_results["integration"].get("deepface", False) else "❌ FAILED"}
- Avatar Generation: {"✅ PASSED" if self.test_results["integration"].get("avatar-generation", False) else "❌ FAILED"}
- Voice Service: {"✅ PASSED" if self.test_results["integration"].get("voice-service", False) else "❌ FAILED"}
- Overall Integration Pass Rate: {integration_pass_rate:.1%}

## 🔒 Security Tests
- SQL Injection Protection: {"✅ PASSED" if self.test_results["security"].get("sql_injection", False) else "❌ FAILED"}
- XSS Protection: {"✅ PASSED" if self.test_results["security"].get("xss_protection", False) else "❌ FAILED"}
- Authentication Security: {"✅ PASSED" if self.test_results["security"].get("authentication", False) else "❌ FAILED"}
- Overall Security Pass Rate: {security_pass_rate:.1%}

## 🤖 ML Model Tests
- Emotion Detection: {"✅ PASSED" if self.test_results["ml_model"].get("emotion_detection", False) else "❌ FAILED"}

## 🎭 End-to-End Tests
"""

        # Add E2E test results
        e2e_results = {
            k: v
            for k, v in self.test_results["integration"].items()
            if k.startswith("e2e_")
        }
        for test_name, result in e2e_results.items():
            status = "✅ PASSED" if result else "❌ FAILED"
            display_name = test_name.replace("e2e_", "").replace("_", " ").title()
            report += f"- {display_name}: {status}\n"

        # Add recommendations
        report += f"""

## 📋 Recommendations
"""

        if unit_pass_rate < 1.0:
            report += "- Fix failing unit tests before deployment\n"
        if integration_pass_rate < 1.0:
            report += "- Resolve integration service connectivity issues\n"
        if security_pass_rate < 1.0:
            report += "- Address security vulnerabilities immediately\n"
        if not self.test_results["ml_model"].get("emotion_detection", False):
            report += "- Improve ML model accuracy and confidence thresholds\n"

        report += "- Ensure all services meet >99% uptime requirements\n"
        report += "- Optimize response times to under 1 second\n"

        # Save report
        with open("test-results/comprehensive-test-report.md", "w") as f:
            f.write(report)

        print("✅ Test report saved to test-results/comprehensive-test-report.md")
        return (
            unit_pass_rate >= 0.95
            and integration_pass_rate >= 0.95
            and security_pass_rate >= 1.0
        )


async def main():
    """Main test execution"""
    print("🚀 Digital Twin Test Framework Starting...")

    framework = DigitalTwinTestFramework()

    # Run all test suites
    await framework.run_unit_tests()
    await framework.run_integration_tests()
    await framework.run_performance_tests()
    await framework.run_security_tests()
    await framework.run_ml_model_tests()
    await framework.run_end_to_end_tests()

    # Generate final report
    success = framework.generate_test_report()

    if success:
        print("\n🎉 All tests completed successfully! Ready for deployment.")
        return 0
    else:
        print("\n⚠️  Some tests failed. Review report and fix issues before deployment.")
        return 1


if __name__ == "__main__":
    exit_code = asyncio.run(main())
    sys.exit(exit_code)
