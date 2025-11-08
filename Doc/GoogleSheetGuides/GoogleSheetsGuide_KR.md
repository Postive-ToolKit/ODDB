# Google Sheets 연동 가이드
구글 시트와 연동하기 위해서는 기본적으로 구글 시트의 Apps Script를 사용해야 합니다. 아래 가이드를 따라 설정을 완료하세요.

## 1. Apps Script 작성하기
ODDB에서는 연동에 사용할 스크립트를 자동으로 생성하는 기능을 제공합니다. 다음 단계를 따라 진행하세요.
1. ODDB Setting 파일의 `Google Sheets Settings` 섹션에서 `Google Sheet API Secret Key`를 설정합니다.
2. ODDB 상단 메뉴에서 `ODDB/Google Sheets/Create App Script for Google Sheets`를 클릭합니다.
3. 스크립트가 생성되면 자동으로 클립보드에 복사됩니다.

## 2. Google Sheets에 Apps Script 추가하기
1. 구글 시트를 엽니다.
2. 상단 메뉴에서 `확장 프로그램 > Apps Script`를 클릭합니다.
3. 새로 열린 Apps Script 편집기에서 기존 코드를 모두 삭제합니다.
4. 클립보드에 복사된 스크립트를 붙여넣습니다.

## 3. Apps Script 배포하기
1. Apps Script 편집기 상단 메뉴에서 `배포 > 새 배포`를 클릭합니다.
2. `배포 유형`에서 `웹 앱`을 선택합니다.
3. `설명` 필드에 배포에 대한 설명을 입력합니다.
4. `앱에 액세스할 수 있는 사용자` 옵션을 `모든 사용자(익명 사용자 포함)`로 설정합니다.
5. `배포` 버튼을 클릭합니다.

## 3.1 권한 승인하기
1. 배포 과정에서 권한 승인이 필요할 수 있습니다. `검토 권한` 버튼을 클릭합니다.
2. Google 계정을 선택합니다.
3. `고급`을 클릭한 후 `안전하지 않음` 링크를 클릭합니다.
4. `허용` 버튼을 클릭하여 권한을 승인합니다.

## 4. 웹 앱 URL 복사하기
1. 배포가 완료되면 표시되는 `웹 앱 URL`을 복사합니다.
2. ODDB Setting 파일의 `Google Sheets Settings` 섹션에서 `Google Sheets API URL" 필드에 복사한 URL을 붙여넣습니다.

---
이제 ODDB와 Google Sheets 간의 연동이 완료되었습니다. ODDB 상단 메뉴에서 `ODDB/Google Sheets/Import from Google Sheets` 및 `ODDB/Google Sheets/Export to Google Sheets` 옵션을 사용하여 데이터를 가져오고 내보낼 수 있습니다.