# Google Sheets API 연동 가이드

ODDB는 Google OAuth 사용자 인증과 Google Sheets API v4를 사용해 Google Sheets를 가져오고 내보냅니다. Apps Script 배포나 서비스 계정 공유는 필요하지 않습니다.

## 1. Google OAuth Client 생성

1. [Google Cloud Console](https://console.cloud.google.com/)에서 프로젝트를 생성하거나 선택합니다.
2. `API 및 서비스 > 라이브러리`에서 `Google Sheets API`를 활성화합니다.
3. OAuth 동의 화면과 대상 사용자를 구성합니다.
4. 애플리케이션 유형이 `데스크톱 앱`인 OAuth 2.0 Client를 생성합니다.
5. 생성된 Client ID와 Client Secret을 복사합니다.

## 2. Spreadsheet 준비

1. 대상 Google Spreadsheet를 엽니다.
2. `공유`를 누릅니다.
3. Unity에서 인증할 Google 계정에 편집자 권한이 있는지 확인합니다.
4. Spreadsheet URL에서 `/d/`와 `/edit` 사이의 Spreadsheet ID를 복사합니다.

## 3. ODDB 설정

1. `ODDBEditorSettings` 에셋을 선택합니다.
2. `Google Sheets Authentication`에 Desktop OAuth Client ID와 Client Secret을 입력하고 `Save OAuth Client`를 누릅니다.
3. `Google Sheets Settings`에서 `Google Spreadsheet Id`를 입력합니다.
4. `Sign in with Google`을 누르고 브라우저 인증을 완료합니다.
5. `Test Connection`으로 인증과 Spreadsheet 접근 권한을 확인합니다.

설정이 완료되지 않은 상태에서 Import 또는 Export를 실행하면 ODDB가 안내창을 표시하고 Inspector에서 `ODDBEditorSettings`를 선택합니다.

OAuth Client 자격 증명은 암호화된 뒤 `ODDBEditorSettings`에 직렬화됩니다. 이는 평문 노출을 방지하지만 ODDB에도 복호화 로직이 포함되므로 저장소 접근 제어를 대체하지는 않습니다. 자격 증명이 들어간 프로젝트 저장소는 Private으로 유지하세요. OAuth 토큰은 사용자 로컬 애플리케이션 데이터 폴더에 저장되며 프로젝트 에셋에는 기록되지 않습니다.

## 4. 동기화

- ODDB Editor의 Import/Export 메뉴에서 `Google Sheets`를 선택합니다.
- 최초 Export 시 기존 Apps Script 형식의 탭을 감지해 ODDB metadata를 추가합니다.
- Export로 연결된 Table과 Google Sheet의 숫자 `sheetId`는 Spreadsheet ID별로 `UserSettings/ODDBGoogleSheetsBindings.json`에 저장됩니다. 이 파일은 프로젝트 로컬 설정이며 에셋이나 OAuth 자격 증명을 포함하지 않습니다.
- 로컬 바인딩이 없거나 손상되면 기존 Google Sheet의 ODDB metadata에서 자동 복구하므로 기존 탭이 중복 생성되지 않습니다. 사용자가 탭 이름을 변경해도 동일한 `sheetId` 연결이 유지됩니다.
- ODDB에서 제거된 관리 컬럼은 다음 Export에서 실제 Google Sheet 컬럼으로 삭제됩니다.
- 삭제 전 확인 창이 표시되며, 사용자 관리 컬럼과 `#` 주석 행은 보존됩니다.
- ODDB에서 제거된 행은 다음 Export에서 실제 Google Sheet 행으로 삭제됩니다. 기존 `#REMOVED` 행도 같은 규칙으로 정리됩니다.
- 행 전체가 삭제되므로 해당 행의 사용자 관리 컬럼 값도 함께 삭제됩니다. 필요한 경우 Google Sheets 버전 기록에서 복구할 수 있습니다.

필드명은 컬럼 식별자로 사용됩니다. 필드명 변경은 기존 컬럼 삭제와 새 컬럼 추가로 처리되므로 해당 컬럼의 수동 서식은 유지되지 않을 수 있습니다.
