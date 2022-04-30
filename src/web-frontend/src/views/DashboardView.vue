<template>
  <div>
    <button @click="callApi">Call</button>
    <p>Stores: {{ storeCount }}</p>
  </div>
</template>

<script>
import axios from "axios";

export default {
  name: "DashboardView",
  data() {
    return {
      storeCount: -1
    };
  },
  methods: {
    async callApi() {
      // Get the access token from the auth wrapper
      const token = await this.$auth.getTokenSilently();
      //alert(token);
      //var userId = this.$auth.user.sub;

      // Use Axios to make a call to the API
      var url = `${process.env.VUE_APP_API_SERVER_URL}/user/current/workspaces`;
      alert(url);
      const { data } = await axios.get(url, {
         headers: {
           Authorization: `Bearer ${token}`    // send the access token through the 'Authorization' header
         }
      });

      this.storeCount = data.length;
    }
  }
};
</script>
