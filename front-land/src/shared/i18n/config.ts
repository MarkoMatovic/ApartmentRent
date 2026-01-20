import i18n from 'i18next';
import { initReactI18next } from 'react-i18next';
import LanguageDetector from 'i18next-browser-languagedetector';

import srCommon from '../../locales/sr/common.json';
import srApartments from '../../locales/sr/apartments.json';
import srAuth from '../../locales/sr/auth.json';
import srRoommates from '../../locales/sr/roommates.json';
import srChat from '../../locales/sr/chat.json';
import srDashboard from '../../locales/sr/dashboard.json';

import enCommon from '../../locales/en/common.json';
import enApartments from '../../locales/en/apartments.json';
import enAuth from '../../locales/en/auth.json';
import enRoommates from '../../locales/en/roommates.json';
import enChat from '../../locales/en/chat.json';
import enDashboard from '../../locales/en/dashboard.json';

import ruCommon from '../../locales/ru/common.json';
import ruApartments from '../../locales/ru/apartments.json';
import ruAuth from '../../locales/ru/auth.json';
import ruRoommates from '../../locales/ru/roommates.json';
import ruChat from '../../locales/ru/chat.json';
import ruDashboard from '../../locales/ru/dashboard.json';

import deCommon from '../../locales/de/common.json';
import deApartments from '../../locales/de/apartments.json';
import deAuth from '../../locales/de/auth.json';
import deRoommates from '../../locales/de/roommates.json';
import deChat from '../../locales/de/chat.json';
import deDashboard from '../../locales/de/dashboard.json';

i18n
  .use(LanguageDetector)
  .use(initReactI18next)
  .init({
    resources: {
      sr: {
        common: srCommon,
        apartments: srApartments,
        auth: srAuth,
        roommates: srRoommates,
        chat: srChat,
        dashboard: srDashboard,
      },
      en: {
        common: enCommon,
        apartments: enApartments,
        auth: enAuth,
        roommates: enRoommates,
        chat: enChat,
        dashboard: enDashboard,
      },
      ru: {
        common: ruCommon,
        apartments: ruApartments,
        auth: ruAuth,
        roommates: ruRoommates,
        chat: ruChat,
        dashboard: ruDashboard,
      },
      de: {
        common: deCommon,
        apartments: deApartments,
        auth: deAuth,
        roommates: deRoommates,
        chat: deChat,
        dashboard: deDashboard,
      },
    },
    fallbackLng: 'sr',
    defaultNS: 'common',
    ns: ['common', 'apartments', 'auth', 'roommates', 'chat', 'dashboard'],
    interpolation: {
      escapeValue: false,
    },
    detection: {
      order: ['localStorage', 'navigator'],
      caches: ['localStorage'],
    },
  });

export default i18n;

