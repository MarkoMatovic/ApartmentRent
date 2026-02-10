import i18n from 'i18next';
import { initReactI18next } from 'react-i18next';
import LanguageDetector from 'i18next-browser-languagedetector';

import srCommon from '../../locales/sr/common.json';
import srApartments from '../../locales/sr/apartments.json';
import srAuth from '../../locales/sr/auth.json';
import srRoommates from '../../locales/sr/roommates.json';
import srChat from '../../locales/sr/chat.json';
import srDashboard from '../../locales/sr/dashboard.json';
import srSupport from '../../locales/sr/support.json';
import srPricing from '../../locales/sr/pricing.json';
import srFooter from '../../locales/sr/footer.json';
import srRoommateAnalytics from '../../locales/sr/roommateAnalytics.json';
import srAnalytics from '../../locales/sr/analytics.json';
import srPremium from '../../locales/sr/premium.json';
import srMessages from '../../locales/sr/messages.json';

import enCommon from '../../locales/en/common.json';
import enApartments from '../../locales/en/apartments.json';
import enAuth from '../../locales/en/auth.json';
import enRoommates from '../../locales/en/roommates.json';
import enChat from '../../locales/en/chat.json';
import enDashboard from '../../locales/en/dashboard.json';
import enSupport from '../../locales/en/support.json';
import enPricing from '../../locales/en/pricing.json';
import enFooter from '../../locales/en/footer.json';
import enRoommateAnalytics from '../../locales/en/roommateAnalytics.json';
import enAnalytics from '../../locales/en/analytics.json';
import enPremium from '../../locales/en/premium.json';
import enMessages from '../../locales/en/messages.json';

import ruCommon from '../../locales/ru/common.json';
import ruApartments from '../../locales/ru/apartments.json';
import ruAuth from '../../locales/ru/auth.json';
import ruRoommates from '../../locales/ru/roommates.json';
import ruChat from '../../locales/ru/chat.json';
import ruDashboard from '../../locales/ru/dashboard.json';
import ruSupport from '../../locales/ru/support.json';
import ruPricing from '../../locales/ru/pricing.json';
import ruFooter from '../../locales/ru/footer.json';
import ruRoommateAnalytics from '../../locales/ru/roommateAnalytics.json';
import ruAnalytics from '../../locales/ru/analytics.json';
import ruPremium from '../../locales/ru/premium.json';
import ruMessages from '../../locales/ru/messages.json';

import deCommon from '../../locales/de/common.json';
import deApartments from '../../locales/de/apartments.json';
import deAuth from '../../locales/de/auth.json';
import deRoommates from '../../locales/de/roommates.json';
import deChat from '../../locales/de/chat.json';
import deDashboard from '../../locales/de/dashboard.json';
import deSupport from '../../locales/de/support.json';
import dePricing from '../../locales/de/pricing.json';
import deFooter from '../../locales/de/footer.json';
import deRoommateAnalytics from '../../locales/de/roommateAnalytics.json';
import deAnalytics from '../../locales/de/analytics.json';
import dePremium from '../../locales/de/premium.json';
import deMessages from '../../locales/de/messages.json';

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
        support: srSupport,
        pricing: srPricing,
        footer: srFooter,
        roommateAnalytics: srRoommateAnalytics,
        analytics: srAnalytics,
        premium: srPremium,
        messages: srMessages,
      },
      en: {
        common: enCommon,
        apartments: enApartments,
        auth: enAuth,
        roommates: enRoommates,
        chat: enChat,
        dashboard: enDashboard,
        support: enSupport,
        pricing: enPricing,
        footer: enFooter,
        roommateAnalytics: enRoommateAnalytics,
        analytics: enAnalytics,
        premium: enPremium,
        messages: enMessages,
      },
      ru: {
        common: ruCommon,
        apartments: ruApartments,
        auth: ruAuth,
        roommates: ruRoommates,
        chat: ruChat,
        dashboard: ruDashboard,
        support: ruSupport,
        pricing: ruPricing,
        footer: ruFooter,
        roommateAnalytics: ruRoommateAnalytics,
        analytics: ruAnalytics,
        premium: ruPremium,
        messages: ruMessages,
      },
      de: {
        common: deCommon,
        apartments: deApartments,
        auth: deAuth,
        roommates: deRoommates,
        chat: deChat,
        dashboard: deDashboard,
        support: deSupport,
        pricing: dePricing,
        footer: deFooter,
        roommateAnalytics: deRoommateAnalytics,
        analytics: deAnalytics,
        premium: dePremium,
        messages: deMessages,
      },
    },
    fallbackLng: 'sr',
    defaultNS: 'common',
    ns: ['common', 'apartments', 'auth', 'roommates', 'chat', 'dashboard', 'support', 'pricing', 'footer', 'roommateAnalytics', 'analytics', 'premium', 'messages'],
    interpolation: {
      escapeValue: false,
    },
    detection: {
      order: ['localStorage', 'navigator'],
      caches: ['localStorage'],
    },
  });

export default i18n;

