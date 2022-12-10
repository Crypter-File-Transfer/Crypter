/*
 * Copyright (C) 2022 Crypter File Transfer
 * 
 * This file is part of the Crypter file transfer project.
 * 
 * Crypter is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Affero General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 * 
 * The Crypter source code is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU Affero General Public License for more details.
 * 
 * You should have received a copy of the GNU Affero General Public License
 * along with this program.  If not, see <http://www.gnu.org/licenses/>.
 * 
 * You can be released from the requirements of the aforementioned license
 * by purchasing a commercial license. Buying such a license is mandatory
 * as soon as you develop commercial activities involving the Crypter source
 * code without disclosing the source code of your own applications.
 * 
 * Contact the current copyright holder to discuss commercial license options.
 */

using BlazorSodium.Services;
using Crypter.ClientServices.DeviceStorage.Enums;
using Crypter.ClientServices.Interfaces;
using Crypter.ClientServices.Interfaces.Enum;
using Crypter.ClientServices.Interfaces.Events;
using Crypter.ClientServices.Interfaces.Repositories;
using Crypter.Common.Monads;
using Crypter.Web.Shared.Modal;
using Microsoft.AspNetCore.Components;
using System;
using System.Threading.Tasks;

namespace Crypter.Web.Shared
{
   public partial class MainLayoutBase : LayoutComponentBase, IDisposable
   {
      [Inject]
      private IBlazorSodiumService BlazorSodiumService { get; set; }

      [Inject]
      private IUserSessionService UserSessionService { get; set; }

      [Inject]
      private IUserKeysService UserKeysService { get; set; }

      [Inject]
      private IUserPasswordService UserPasswordService { get; set; }

      [Inject]
      private IDeviceRepository<BrowserStorageLocation> BrowserRepository { get; set; }

      public BasicModal BasicModal { get; protected set; }
      public RecoveryKeyModal RecoveryKeyModal { get; protected set; }
      public TransferSuccessModal TransferSuccessModal { get; protected set; }
      public SpinnerModal SpinnerModal { get; protected set; }

      protected bool ServicesInitialized { get; set; }

      protected override async Task OnInitializedAsync()
      {
         UserSessionService.UserLoggedInEventHandler += HandleUserLoggedInEvent;
         UserPasswordService.PasswordHashBeginEventHandler += ShowPasswordHashingModal;
         UserPasswordService.PasswordHashEndEventHandler += ClosePasswordHashingModal;
         await BlazorSodiumService.InitializeAsync();
         await BrowserRepository.InitializeAsync();
         ServicesInitialized = true;
      }

      private async void HandleUserLoggedInEvent(object sender, UserLoggedInEventArgs args)
      {
         await UserPasswordService.DeriveUserCredentialKeyAsync(args.Username, args.Password, UserPasswordService.CurrentPasswordVersion)
            .BindAsync(async credentialKey =>
            {
               if (args.UploadNewKeys)
               {
                  await UserKeysService.UploadNewKeysAsync(args.Username, args.VersionedPassword, credentialKey, args.RememberUser);
               }
               else
               {
                  await UserKeysService.DownloadExistingKeysAsync(credentialKey, args.RememberUser);
               }

               if (args.ShowRecoveryKeyModal)
               {
                  await RecoveryKeyModal.OpenAsync(args.Username, args.VersionedPassword);
               }
            });
      }

      private void ShowPasswordHashingModal(object sender, PasswordHashBeginEventArgs args)
      {
         switch (args.HashType)
         {
            case PasswordHashType.AuthenticationKey:
               SpinnerModal.Open("Securing your Password", "Please wait while your password is securely hashed. Your real password is never provided to the server.", Maybe<EventCallback>.None);
               break;
            case PasswordHashType.CredentialKey:
               SpinnerModal.Open("Calculating Encryption Key", "Please wait while your personal encryption key is calculated.", Maybe<EventCallback>.None);
               break;
         }
      }

      private async void ClosePasswordHashingModal(object sender, PasswordHashEndEventArgs args)
      {
         await SpinnerModal.CloseAsync();
      }

      public void Dispose()
      {
         UserSessionService.UserLoggedInEventHandler -= HandleUserLoggedInEvent;
         UserPasswordService.PasswordHashBeginEventHandler -= ShowPasswordHashingModal;
         UserPasswordService.PasswordHashEndEventHandler -= ClosePasswordHashingModal;
         GC.SuppressFinalize(this);
      }
   }
}
