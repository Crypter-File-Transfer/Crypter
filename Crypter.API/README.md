# Crypter.API

## Endpoint Listing

Seeing all the endpoints at a glance helps to reveal how the API is organized.
This should also aid developers in deciding where to add a new API endpoint or whether to add a new controller.

* File Transfer
    * POST /api/file/transfer?username={username}
    * GET /api/file/transfer/sent
    * GET /api/file/transfer/received
    * GET /api/file/transfer/preview/anonymous?id={hashId}
    * GET /api/file/transfer/preview/user?id={hashId}
    * GET /api/file/transfer/ciphertext/anonymous?id={hashId}&proof={proof}
    * GET /api/file/transfer/ciphertext/user?id={hashId}&proof={proof}

* Message Transfer
   * POST /api/message/transfer?username={username}
   * GET /api/message/transfer/sent
   * GET /api/message/transfer/received
   * GET /api/message/transfer/preview/anonymous?id={hashId}
   * GET /api/message/transfer/user/anonymous?id={hashId}
   * GET /api/message/transfer/ciphertext/anonymous?id={hashId}&proof={proof}
   * GET /api/message/transfer/ciphertext/user?id={hashId}&proof={proof}

* User
   * GET /api/user/profile?username={username}
   * GET /api/user/search?keyword={keyword}&index={index}&count={count}

* User Authentication
   * POST /api/user/authentication/register
   * POST /api/user/authentication/login
   * GET /api/user/authentication/refresh
   * POST /api/user/authentication/logout
   * POST /api/user/authentication/password/challenge

* User Contacts
   * GET /api/user/contact
   * POST /api/user/contact?username={username}
   * DELETE /api/user/contact?username={username}

* User Consents
   * POST /api/user/consent/recovery-key-risk

* User Keys
   * GET /api/user/key/master
   * PUT /api/user/key/master
   * POST /api/user/key/master/recovery-proof/challenge
   * GET /api/user/key/private
   * PUT /api/user/key/private

* User Recovery
   * GET /api/user/recovery?email={emailAddress}
   * POST /api/user/recovery

* User Settings
   * GET /api/user/setting/profile
   * PUT /api/user/setting/profile
   * GET /api/user/setting/contact
   * POST /api/user/setting/contact
   * POST /api/user/setting/contact/verify
   * GET /api/user/setting/notification
   * POST /api/user/setting/notification
   * GET /api/user/setting/privacy
   * PUT /api/user/setting/
   * GET /api/user/setting/transfer

* Version
   * GET /api/version
