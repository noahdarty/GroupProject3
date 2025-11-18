// Firebase Configuration
// Replace these values with your Firebase project config

import { initializeApp } from 'https://www.gstatic.com/firebasejs/10.7.1/firebase-app.js';
import { getAuth } from 'https://www.gstatic.com/firebasejs/10.7.1/firebase-auth.js';

// TODO: Replace with your Firebase config
const firebaseConfig = {
  apiKey: "AIzaSyC0M4hqTF98RoyFigMsWQUQyfZDROZtcgA",
  authDomain: "vulnradar-e3865.firebaseapp.com",
  projectId: "vulnradar-e3865",
  storageBucket: "vulnradar-e3865.firebasestorage.app",
  messagingSenderId: "140271150507",
  appId: "1:140271150507:web:5dad8571519ab2d4571eee"
};

// Initialize Firebase
const app = initializeApp(firebaseConfig);
export const auth = getAuth(app);

