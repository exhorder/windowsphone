﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Navigation;
using mega;
using MegaApp.Classes;
using MegaApp.Enums;
using MegaApp.Pages;
using MegaApp.Resources;
using MegaApp.Services;
using Microsoft.Devices.Sensors;
using Microsoft.Phone.Controls;

namespace MegaApp.Models
{
    class LoginViewModel : BaseRequestListenerViewModel
    {
        private readonly MegaSDK _megaSdk;

        public LoginViewModel(MegaSDK megaSdk)
        {
            this._megaSdk = megaSdk;
            this.StayLoggedIn = SettingsService.LoadSetting<bool>(SettingsResources.StayLoggedIn, true);
            this.ControlState = true;
            this.NavigateCreateAccountCommand = new DelegateCommand(NavigateCreateAccount);
        }

        #region Methods

        public void DoLogin()
        {
            if (CheckInputParameters())
            {
                this._megaSdk.login(Email, Password, this);
            }
            else
            {
                MessageBox.Show(AppMessages.RequiredFieldsLogin, AppMessages.RequiredFields_Title,
                        MessageBoxButton.OK);
            }
        }
        private static void NavigateCreateAccount(object obj)
        {
            NavigateService.NavigateTo(typeof(CreateAccountPage), NavigationParameter.Normal);
        }

        private bool CheckInputParameters()
        {
            return !String.IsNullOrEmpty(Email) && !String.IsNullOrEmpty(Password);
        }

        private static void SaveLoginData(string email, string session, bool stayLoggedIn)
        {
            SettingsService.SaveMegaLoginData(email, session, stayLoggedIn);
        }
        
        #endregion

        #region Commands

        public ICommand NavigateCreateAccountCommand { get; set; }

        #endregion

        #region Properties

        public string Email { get; set; }
        public string Password { get; set; }
        public bool StayLoggedIn { get; set; }
        public string SessionKey { get; private set; }

        #endregion

        #region  Base Properties

        protected override string ProgressMessage
        {
            get { return ProgressMessages.Login; }
        }

        protected override string ErrorMessage
        {
            get { return AppMessages.LoginFailed; }
        }

        protected override string ErrorMessageTitle
        {
            get { return AppMessages.LoginFailed_Title; }
        }

        protected override string SuccessMessage
        {
            get { throw new NotImplementedException(); }
        }

        protected override string SuccessMessageTitle
        {
            get { throw new NotImplementedException(); }
        }

        protected override bool ShowSuccesMessage
        {
            get { return false; }
        }

        protected override bool NavigateOnSucces
        {
            get { return true; }
        }

        protected override bool ActionOnSucces
        {
            get { return true; }
        }

        protected override Type NavigateToPage
        {
            get { return typeof(MainPage); }
        }

        protected override NavigationParameter NavigationParameter
        {
            get { return NavigationParameter.Login; }
        }

        #endregion

        #region MRequestListenerInterface

        public override void onRequestFinish(MegaSDK api, MRequest request, MError e)
        {
            if (e.getErrorCode() == MErrorType.API_OK)
                SessionKey = api.dumpSession();

            base.onRequestFinish(api, request, e);
        }

        #endregion

        #region Override Methods

        protected override void OnSuccesAction(MRequest request)
        {
            SaveLoginData(Email, SessionKey, StayLoggedIn);
        }

        #endregion
        
        
    }
}