# Google Sheets API 연동 가이드

ODDB는 Google Sheets API v4와 서비스 계정을 사용해 Google Sheets를 가져오고 내보냅니다. Apps Script 배포는 필요하지 않습니다.

## 1. Google Cloud 설정

1. [Google Cloud Console](https://console.cloud.google.com/)에서 프로젝트를 생성하거나 선택합니다.
2. `API 및 서비스 > 라이브러리`에서 `Google Sheets API`를 활성화합니다.
3. `IAM 및 관리자 > 서비스 계정`에서 서비스 계정을 생성합니다.
4. 서비스 계정의 JSON 키를 생성해 안전한 로컬 폴더에 저장합니다.

Credential JSON에는 개인 키가 포함됩니다. `Assets` 폴더나 버전 관리 저장소에 넣지 마세요.

## 2. Spreadsheet 공유

1. 대상 Google Spreadsheet를 엽니다.
2. `공유`를 누릅니다.
3. 서비스 계정 JSON의 `client_email` 주소를 편집자로 추가합니다.
4. Spreadsheet URL에서 `/d/`와 `/edit` 사이의 Spreadsheet ID를 복사합니다.

## 3. ODDB 설정

1. `ODDBEditorSettings`의 `Google Sheets Settings`에서 `Google Spreadsheet Id`를 입력합니다.
2. Unity 메뉴에서 `ODDB > Google Sheets > Setup`을 엽니다.
3. `Browse`를 눌러 서비스 계정 JSON을 선택합니다.
4. `Test Connection`으로 인증과 공유 권한을 확인합니다.

Credential 경로는 사용자별 `EditorPrefs`에 저장되며 프로젝트 에셋에는 기록되지 않습니다.

## 4. 동기화

- ODDB Editor의 Import/Export 메뉴에서 `Google Sheets`를 선택합니다.
- 최초 Export 시 기존 Apps Script 형식의 탭을 감지해 ODDB metadata를 추가합니다.
- ODDB에서 제거된 관리 컬럼은 다음 Export에서 실제 Google Sheet 컬럼으로 삭제됩니다.
- 삭제 전 확인 창이 표시되며, 사용자 관리 컬럼과 `#` 주석 행은 보존됩니다.
- 제거된 행은 기존 동작과 동일하게 `#REMOVED`로 표시됩니다.

필드명은 컬럼 식별자로 사용됩니다. 필드명 변경은 기존 컬럼 삭제와 새 컬럼 추가로 처리되므로 해당 컬럼의 수동 서식은 유지되지 않을 수 있습니다.
