# 기존 QoLEasyReading GitHub 저장소 갱신 안내 (S3-QC-HF1)

**현재 ZIP은 1.6/1.7 최신 소스를 포함하지만 실행 DLL은 1.7 최신 Release만 포함합니다.** 1.6 구형 DLL을 삭제한 상태이므로 이전 GitHub 저장소에서 오래된 DLL이 남아 있지 않게 주의하세요.

## 적용

1. 기존 로컬 Git 저장소를 백업하고 `.git/` 폴더는 그대로 보존합니다.
2. `QoLEasyReading_GitHub_v1.1.0_QCHF1_SOURCE_RW17_BINARY_REPOSITORY.zip`을 풀어 `QoLEasyReading/` **안의 내용**을 기존 Git 저장소 루트에 덮어씁니다.
3. 기존 저장소에서 추적 중인 아래 **구형 1.6 DLL을 명시적으로 삭제**합니다(파일이 추적 중일 때):

   ```bash
   git rm Mod/QoLEasyReading/1.6/Assemblies/QoLEasyReading.dll
   ```

4. 다음 결과에 1.6 DLL이 다시 추가되지 않았는지 확인합니다.

   ```bash
   git status --short
   git add -A
   git diff --cached --stat
   git diff --cached --name-status
   ```

5. `.git/`, 사용자 경로 `.props`, 게임 DLL, PDB, `bin/obj`가 스테이징되지 않았는지 확인한 뒤 커밋·푸시합니다. GitHub의 기존 태그 `v1.1.0`이 이미 존재한다면 강제로 옮기지 말고 별도의 새 버전/태그로 배포하세요.

## 파일 이름

- 저장소 전체: `QoLEasyReading_GitHub_v1.1.0_QCHF1_SOURCE_RW17_BINARY_REPOSITORY.zip`
- **1.7 설치용만**: `QoLEasyReading-v1.1.0-RW17-QCHF1.zip`
- 해시 및 검증: `QoLEasyReading_v1.1.0_QCHF1_PACKAGING_VERIFICATION_20261117.json`

새 1.6 Release DLL을 빌드·검증하기 전까지 **통합 설치 ZIP을 만들거나 1.6 동작 보장을 표시하지 마세요.** GitHub 커밋·푸시는 이 ZIP 제작 과정에서 실행하지 않았습니다.
