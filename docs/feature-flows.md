# Feature Flows

## User Registration & Onboarding
1. **Register**: User clicks register button
2. **Submit**: User completes minimal Auth0 form for registration.
3. **Persist**: Create `User` record with `status: PENDING`.
4. **Trigger**: API emits `USER_CREATED` event.
5. **Notify**: Worker picks up event and sends "Verify Email" via SendGrid.


